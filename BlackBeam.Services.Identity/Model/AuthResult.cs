namespace BlackBeam.Services.Identity.Models
{
    public record AuthResult(bool IsSuccess, string? Token, string? ErrorMessage);
}