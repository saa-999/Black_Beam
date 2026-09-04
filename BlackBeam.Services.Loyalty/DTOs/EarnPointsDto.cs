namespace BlackBeam.Services.Loyalty.DTOs
{
    public class EarnPointsDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public decimal MonetaryValue { get; set; }
    }
}
