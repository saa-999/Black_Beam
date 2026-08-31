using BlackBeam.Shared;
namespace BlackBeam.Services.Orders.DTOs;

public record CreateOrderRequest(string  CashierId, string PaymentMethod, List<OrderItemDto> Items);
public record OrderItemDto(string Barcode, string ProductName, decimal UnitPrice, int Quantity);