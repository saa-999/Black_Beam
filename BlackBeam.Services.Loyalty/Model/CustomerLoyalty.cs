using BlackBeam.Services.Loyalty.Enum;
namespace BlackBeam.Services.Loyalty.Model
{
    public class CustomerLoyalty
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string PhoneNumber { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public Tier Tier { get; set; } = Tier.Bronze;
    }
}
