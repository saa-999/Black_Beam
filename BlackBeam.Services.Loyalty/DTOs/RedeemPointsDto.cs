namespace BlackBeam.Services.Loyalty.DTOs
{
    public class RedeemPointsDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public decimal AmountToPay { get; set; }
    }
}
