using System.Net.Http.Json;
using System.Net.Http.Headers;
using BlackBeam.Services.Orders.Data;
using BlackBeam.Services.Orders.DTOs;
using BlackBeam.Services.Orders.Model;
using BlackBeam.Shared.Responses;
using BlackBeam.Shared;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using BlackBeam.Shared.EnumRole;
using Stripe;
using Stripe.Checkout;
using Microsoft.AspNetCore.SignalR;
using BlackBeam.Services.Orders.Enum;

namespace BlackBeam.Services.Orders.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubContext<CashierHub> _hubContext;
    private readonly OrderTrackingManager _tracker;

    public OrderService(IHttpClientFactory httpClientFactory, OrderDbContext db, IHttpContextAccessor httpContextAccessor , IHubContext<CashierHub> hubContext , OrderTrackingManager tracker)
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
        var payload = request.Items.Select(i => new
        {
            Barcode = i.Barcode,
            Quantity = i.Quantity
        }).ToList();

        string? authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization;

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return ApiResponse<Guid>.Failure(["حدث خطأ في المصادقة"]);
        }

        string jwtToken = authHeader.Substring(7);
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null)
        {
            return ApiResponse<Guid>.Failure(["بيانات المستخدم غير متوفرة"]);
        }

        bool isStaff = user.IsInRole(EnumRole.Admin) || user.IsInRole(EnumRole.Cashier);
        if (!isStaff)
        {
            return ApiResponse<Guid>.Failure(["لا تملك الصلاحية لإتمام هذه العملية"]);
        }

        string endpoint = "/api/Inventory/DeductStock";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

        var response = await _httpClient.PutAsJsonAsync(endpoint, payload);

        if (!response.IsSuccessStatusCode)
        {
            var errorData = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
            return ApiResponse<Guid>.Failure(errorData?.Error ?? ["فشل الاتصال بخدمة المخزون"]);
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
            }).ToList()
        };

        _db.order.Add(order);
        await _db.SaveChangesAsync();
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
            status = Enum.EnumOrderStatus.Pending
        };
        _db.order.Add(order);
        await _db.SaveChangesAsync();

        if(request.PaymentMethod != EnumPayMethod.Card)
        {
            await _hubContext.Clients.Group("Cashiers").SendAsync("ReceiveNewOrder",order);
            return ApiResponse<string>.Success("تم رفع الطلب، بانتظار تسليم الكاشير");
        }
        var context = new PayResult<Order> {Value = order};
        var pay = await PayAsync(context);

        if (!pay.IsSuccess)
        {
            _db.order.Remove(order);
            await _db.SaveChangesAsync();
            return ApiResponse<string>.Failure([$"{pay.Error}"]);
        }
         await _hubContext.Clients.Group("Cashiers").SendAsync("ReceiveNewOrder",order);
        return ApiResponse<string>.Success(pay.Value);

    }
    public async Task<ApiResponse<bool>> ConfirmOrderAndDeductInventoryAsync(Guid orderId ,EnumOrderStatus status)
    {
        var order = await _db.order.FindAsync(orderId);
        if(order == null)
        {
            return ApiResponse<bool>.Failure(["رقم اطلب غير متوفر"]);
        }

        order.status =  status;
        await _db.SaveChangesAsync();
         _tracker.ReleaseOrder(order.Id);
        return ApiResponse<bool>.Success(true, "تم تسليم الطلب وخصم المخزون بنجاح");
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
                {"CashierId",context.Value.CashierId}
            },
            AutomaticTax = new SessionAutomaticTaxOptions { Enabled = true },
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
}