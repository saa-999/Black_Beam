using BlackBeam.Shared;
namespace BlackBeam.Services.Orders.DTOs;

public record CreateOrderRequest(string? CashierId, string PaymentMethod, List<OrderItemDto> Items, bool IsPaid);
/**
 fix: i remov UnitPrice because security reasons , we don't want to expose the unit price to the client side
      we will get the unit price from the Inventory service when we create the order, and we will calculate the total price based on the unit price and quantity
 */
public record OrderItemDto(string Barcode, string ProductName, int Quantity);