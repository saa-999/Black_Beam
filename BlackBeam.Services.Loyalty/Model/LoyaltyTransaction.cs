using BlackBeam.Services.Loyalty.Enum;
namespace BlackBeam.Services.Loyalty.Model
{
    public class LoyaltyTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public  int  Points { get; set; }
        public OperationType Type { get; set; }
        public decimal MonetaryValue { get; set; }
    }
}
