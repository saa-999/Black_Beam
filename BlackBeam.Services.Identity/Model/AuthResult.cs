namespace BlackBeam.Services.Identity.Models
{
    public record AuthResult(bool IsSuccess, string? Token, string? ErrorMessage);
    public record LoginRequest(string PhoneNumber, string Password);
    public record RegistRequest(string PhoneNumber, string Password, string Name);
}