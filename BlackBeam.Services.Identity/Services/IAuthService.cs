using BlackBeam.Services.Identity.Models;
using BlackBeam.Services.Identity.Endpoints;



namespace BlackBeam.Services.Identity.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<AuthResult> RegisterCustomerAsync(RegistRequest request);
        Task<AuthResult> RegisterCashierAsync(RegisterStaffRequest request);
    }
}