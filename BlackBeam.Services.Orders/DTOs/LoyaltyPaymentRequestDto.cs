using BlackBeam.Services.Orders.Enum;

namespace BlackBeam.Services.Orders.DTOs
{
    public class LoyaltyPaymentRequestDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public LoyaltyOperationType Operation { get; set; }
        public decimal Price { get; set; }
        public Guid OrderId { get; set; }
    }
}
