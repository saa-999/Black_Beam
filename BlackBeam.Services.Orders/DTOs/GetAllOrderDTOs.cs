using BlackBeam.Services.Orders.Enum;
using BlackBeam.Shared;

namespace BlackBeam.Services.Orders.DTOs
{
    public class GetAllOrderDTOs
    {
        public Guid Id { get; set; } 
        public string? CashierId { get; set; }
        public decimal TotalAmount { get; set; }
        public EnumOrderStatus status { get; set; } 
        public DateTime OrderDate { get; set; } 
        public string? PaymentMethod { get; set; }
        public bool IsPaid { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductBarcode { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }

    }
}
