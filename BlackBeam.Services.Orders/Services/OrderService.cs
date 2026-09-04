using BlackBeam.Services.Orders.Data;
using BlackBeam.Services.Orders.DTOs;
using BlackBeam.Services.Orders.Enum;
using BlackBeam.Services.Orders.Model;
using BlackBeam.Shared;
using BlackBeam.Shared.EnumRole;
using BlackBeam.Shared.Responses;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Collections;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace BlackBeam.Services.Orders.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubContext<CashierHub> _hubContext;
    private readonly OrderTrackingManager _tracker;

    public OrderService(IHttpClientFactory httpClientFactory, OrderDbContext db, IHttpContextAccessor httpContextAccessor, IHubContext<CashierHub> hubContext, OrderTrackingManager tracker)
    {
        _db = db;
        _httpClient = httpClientFactory.CreateClient("InventoryClient");
        _httpContextAccessor = httpContextAccessor;
        _hubContext = hubContext;
        _tracker = tracker;
    }
    public async Task<ApiResponse<Guid>> CreateOrderAsync(CreateOrderRequest request)
    {
        if (!request.PaymentMethod.IsValidPayment())
        {
            return ApiResponse<Guid>.Failure([$"طريقة الدفع غير معرفه {request.PaymentMethod}"]);
        }


        var order = new Order
        {
            CashierId = request.CashierId,
            PaymentMethod = request.PaymentMethod,
            TotalAmount = request.Items.Sum(i => i.UnitPrice * i.Quantity),
            OrderItems = request.Items.Select(i => new OrderItem
            {
                ProductBarcode = i.Barcode,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                SubTotal = i.UnitPrice * i.Quantity
            }).ToList(),
            status = Enum.EnumOrderStatus.Completed,
            IsPaid = request.IsPaid
        };

        _db.order.Add(order);
        await _db.SaveChangesAsync();

        var deductResult = await DeductInventoryStockAsync(order);
        if (!deductResult.IsSuccess)
        {
            _db.order.Remove(order);
            await _db.SaveChangesAsync();
            return ApiResponse<Guid>.Failure(deductResult.Error);
        }

        return ApiResponse<Guid>.Success(order.Id);
    }

    public async Task<ApiResponse<string>> CreateCustomerOrderAsync(CreateOrderRequest request)
    {
        if (!request.PaymentMethod.IsValidPayment())
        {
            return ApiResponse<string>.Failure([$"طريقة الدفع غير معرفه {request.PaymentMethod}"]);
        }

        var order = new Order
        {
            PaymentMethod = request.PaymentMethod,
            TotalAmount = request.Items.Sum(i => i.UnitPrice * i.Quantity),
            OrderItems = request.Items.Select(i => new OrderItem
            {
                ProductBarcode = i.Barcode,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                SubTotal = i.UnitPrice * i.Quantity
            }).ToList(),
            status = Enum.EnumOrderStatus.Pending,
            IsPaid = false
        };
        _db.order.Add(order);
        await _db.SaveChangesAsync();

        if (request.PaymentMethod != EnumPayMethod.Card)
        {
            await _hubContext.Clients.Group("Cashiers").SendAsync("ReceiveNewOrder", order);
            return ApiResponse<string>.Success("تم رفع الطلب، بانتظار تسليم الكاشير");
        }
        var context = new PayResult<Order> { Value = order };
        var pay = await PayAsync(context);
        if (!pay.IsSuccess)
        {
            _db.order.Remove(order);
            await _db.SaveChangesAsync();
            return ApiResponse<string>.Failure([$"{pay.Error}"]);
        }
        return ApiResponse<string>.Success(pay.Value);
    }
    public async Task<ApiResponse<bool>> ConfirmOrderAndDeductInventoryAsync(Guid orderId, EnumOrderStatus status)
    {
        var order = await _db.order.FindAsync(orderId);
        if (order == null)
        {
            return ApiResponse<bool>.Failure(["رقم اطلب غير متوفر"]);
        }

        var deductResult = await DeductInventoryStockAsync(order);
        if (!deductResult.IsSuccess)
        {
            _tracker.ReleaseOrder(order.Id);
            return ApiResponse<bool>.Failure(deductResult.Error);
        }

        order.status = status;
        order.IsPaid = true;
        await _db.SaveChangesAsync();

        _tracker.ReleaseOrder(order.Id);
        return ApiResponse<bool>.Success(true, "تم تسليم الطلب وخصم المخزون بنجاح");
    }
    public async Task<ApiResponse<bool>> FulfillStripeOrderAsync(Guid orderId)
    {
        var order = await _db.order.FindAsync(orderId);
        if (order == null)
        {
            return ApiResponse<bool>.Failure(["رقم اطلب غير متوفر"]);
        }
        order.IsPaid = true;
        await _db.SaveChangesAsync();
        await _hubContext.Clients.Group("Cashiers").SendAsync("ReceiveNewOrder", order);
        return ApiResponse<bool>.Success(true, "تم تأكيد الدفع بنجاح");
    }
    private async Task<PayResult<string>> PayAsync(PayResult<Order> context)
    {
        if (context == null)
        {
            return PayResult<string>.Failure(["حدث خطاء "]);
        }
        var lineitem = new List<SessionLineItemOptions>();

        foreach (var item in context.Value!.OrderItems)
        {
            lineitem.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(item.UnitPrice * 100),
                    Currency = "sar",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductName,
                        Metadata = new Dictionary<string, string>
                       {
                           {"Barcode", item.ProductBarcode}
                       }
                    }
                },
                Quantity = item.Quantity
            });
        }
        var opt = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            ClientReferenceId = context.Value.Id.ToString(),
            Metadata = new Dictionary<string, string>
            {
                {"CashierId",context.Value.CashierId ?? string.Empty},
                {"OrderId",context.Value.Id.ToString()}
            },
            AutomaticTax = new SessionAutomaticTaxOptions { Enabled = false }, 
            InvoiceCreation = new SessionInvoiceCreationOptions { Enabled = true },
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            Mode = "payment",
            LineItems = lineitem,

            SuccessUrl = "https://www.google.com/",
            CancelUrl = "https://www.google.com/"
        };

        var service = new SessionService();
        var requestOptions = new RequestOptions
        {
            IdempotencyKey = context.Value.Id.ToString()
        };

        try
        {
            Session session = await service.CreateAsync(opt, requestOptions);
            return PayResult<string>.Success(session.Url);
        }
        catch (StripeException ex)
        {
            switch (ex.HttpStatusCode)
            {
                case System.Net.HttpStatusCode.BadRequest:
                    return PayResult<string>.Failure([$"{ex.HttpStatusCode} : {ex.Message}"]);

                case System.Net.HttpStatusCode.Unauthorized:
                    return PayResult<string>.Failure([$"{ex.HttpStatusCode} : {ex.Message}"]);

                case System.Net.HttpStatusCode.TooManyRequests:
                    return PayResult<string>.Failure([$"{ex.HttpStatusCode} : {ex.Message}"]);
                default:
                    return PayResult<string>.Failure([$"{ex.HttpStatusCode} : {ex.Message}"]);
            }
        }
        catch (TaskCanceledException ex)
        {
            return PayResult<string>.Failure([$"{ex.Message}"]);
        }
        catch (Exception ex)
        {
            return PayResult<string>.Failure([$"{ex.Message}"]);
        }
    }

    public async Task<ApiResponse<Order>> GetOrderByIdAsync(Guid orderId)
    {
        var order = await _db.order.FindAsync(orderId);
        if (order == null)
        {
            return ApiResponse<Order>.Failure(["رقم اطلب غير متوفر"]);
        }
        return ApiResponse<Order>.Success(order);
    }
    
    public async Task<ApiResponse<IEnumerable<GetAllOrderDTOs>>> GetAllOrderAsync(int pageNumber  , int pageSize )
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize > 100 ? 100 : (pageSize < 1 ? 20 : pageSize);
        int skip = (pageNumber - 1) * pageSize;
        var allOrder = await _db.order
            .AsNoTracking()
            .Where(o => o.IsPaid)
            .Select(o => new GetAllOrderDTOs
            {
                CashierId = o.CashierId,
                Id = o.Id,
                PaymentMethod = o.PaymentMethod,
                IsPaid = o.IsPaid,
                OrderDate = o.OrderDate,
                ProductBarcode = string.Join(", ", o.OrderItems.Select(oi => oi.ProductBarcode)),
                ProductName = string.Join(", ", o.OrderItems.Select(oi => oi.ProductName)),
                Quantity = o.OrderItems.Sum(oi => oi.Quantity),
                status = o.status,
                SubTotal = o.OrderItems.Sum(oi => oi.SubTotal),
                TotalAmount = o.TotalAmount,
                UnitPrice = o.OrderItems.Sum(oi => oi.UnitPrice),
            })
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        return ApiResponse<IEnumerable<GetAllOrderDTOs>>.Success(allOrder); ;
    }
    public async Task<ApiResponse<IEnumerable<OrderResults>>> SearchOrderAsync(string search)
    {
        string cleanSearch = search.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(cleanSearch))
        {
            return ApiResponse<IEnumerable<OrderResults>>.Failure(["يجب تعبئت خانة البحث"]);
        }

        var order = await _db.order
            .AsNoTracking()
            .Where(o =>
                o.Id.ToString().Contains(cleanSearch)
                ||
                o.OrderItems.Any(oi => EF.Functions.Match(oi.ProductName, cleanSearch, MySqlMatchSearchMode.NaturalLanguage) > 0)
            ).
            Select(o => new OrderResults
            {
                CashierId = o.CashierId,
                Id = o.Id,
                PaymentMethod = o.PaymentMethod,
                IsPaid = o.IsPaid,
                OrderDate = o.OrderDate,
                ProductBarcode = string.Join(", ", o.OrderItems.Select(oi => oi.ProductBarcode)),
                ProductName = string.Join(", ", o.OrderItems.Select(oi => oi.ProductName)),
                Quantity = o.OrderItems.Sum(oi => oi.Quantity),
                status = o.status,
                SubTotal = o.OrderItems.Sum(oi => oi.SubTotal),
                TotalAmount = o.TotalAmount,
                UnitPrice = o.OrderItems.Sum(oi => oi.UnitPrice),
            })
            .Take(10)
            .ToListAsync();

        return ApiResponse<IEnumerable<OrderResults>>.Success(order);
    }
    private async Task<ApiResponse<bool>> DeductInventoryStockAsync(Order order)
    {
        string? authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization;

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return ApiResponse<bool>.Failure(["حدث خطأ في المصادقة: الـ Token غير موجود"]);
        }

        var user = _httpContextAccessor.HttpContext?.User;
         
        if (user == null  || !user.IsInRole(EnumRole.Admin) && !user.IsInRole(EnumRole.Cashier))
        {
            return ApiResponse<bool>.Failure(["لا تملك الصلاحية لإتمام هذه العملية"]);
        }

        string jwtToken = authHeader.Substring(7);

        var payload = order.OrderItems.Select(i => new
        {
            Barcode = i.ProductBarcode,
            Quantity = i.Quantity
        }).ToList();
        // I will move the http://Localhost:5275 to Program.cs file and use it as a configuration value, but for now, I will keep it here.
        string endpoint = "http://localhost:5275/api/Inventory/DeductStock";
        var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            /*
             i will fix error for read from json async in the future, but for now, i will return a generic error message
             */

            //var errorData = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
            //errorData?.Error ??
            return ApiResponse<bool>.Failure( ["فشل الاتصال بخدمة المخزون"]);
        }
        return ApiResponse<bool>.Success(true);
    }
}