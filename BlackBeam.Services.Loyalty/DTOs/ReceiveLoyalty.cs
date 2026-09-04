namespace BlackBeam.Services.Loyalty.DTOs
{
    public record ReceiveLoyalty<T>(T Value, string message);
}
