namespace BlackBeam.Services.Identity.Models
{
    public record AuthResult<T>(bool IsSuccess, T? Data, string? ErrorMessage);
    public record LoginRequest(string  Identifier, string Password);
    public record RegistRequest(string PhoneNumber, string Password, string Name );
    public record RegisterStaffRequest(string Username, string Password, string Name, string? PhoneNumber);
    public record UpdateInformationRequest(
     string Name , string? PhoneNumber,
     string? Username, string? NewUsername, 
     string? Password ,string? NewPassword , 
     bool? IsActive , string? NewPhoneNumber);
}