using BlackBeam.Shared.Responses;
using BlackBeam.Services.Orders.DTOs;
using BlackBeam.Services.Orders.Enum;
using BlackBeam.Services.Orders.Model;
using System.Collections;

namespace BlackBeam.Services.Orders.Services;

public interface IOrderService
{
    Task<ApiResponse<Guid>> CreateOrderAsync(CreateOrderRequest request);
    Task<ApiResponse<string>> CreateCustomerOrderAsync(CreateOrderRequest request);
    Task<ApiResponse<bool>> ConfirmOrderAndDeductInventoryAsync(Guid orderId ,EnumOrderStatus status);
    Task<ApiResponse<bool>> FulfillStripeOrderAsync(Guid orderId);
    Task<ApiResponse<Order>> GetOrderByIdAsync(Guid orderId);
   Task<ApiResponse<IEnumerable<GetAllOrderDTOs>>> GetAllOrderAsync(int pageNumper, int pageSize );
   Task<ApiResponse<IEnumerable<OrderResults>>> SearchOrderAsync(string search);
}