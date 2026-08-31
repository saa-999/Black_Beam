using BlackBeam.Shared.Responses;
using BlackBeam.Services.Orders.DTOs;
using BlackBeam.Services.Orders.Enum;
namespace BlackBeam.Services.Orders.Services;

public interface IOrderService
{
    Task<ApiResponse<Guid>> CreateOrderAsync(CreateOrderRequest request);
    Task<ApiResponse<string>> CreateCustomerOrderAsync(CreateOrderRequest request);
    Task<ApiResponse<bool>> ConfirmOrderAndDeductInventoryAsync(Guid orderId ,EnumOrderStatus status);
}