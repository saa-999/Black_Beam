using BlackBeam.Services.Identity.Models;
using BlackBeam.Services.Identity.Endpoints;
using BlackBeam.Services.Identity.DTOs;


namespace BlackBeam.Services.Identity.Services
{
    public interface IAuthService
    {
        Task<AuthResult<string>> LoginAsync(LoginRequest request);
        Task<AuthResult<string>> RegisterCustomerAsync(RegistRequest request);
        Task<AuthResult<string>> RegisterCashierAsync(RegisterStaffRequest request);
        Task<AuthResult<IEnumerable<EmployeeDto>>> GetAllEmployeesAsync(int pageNumber, int pageSize);
        Task<AuthResult<EmployeeDto>> UpdateEmployeeInformationAsync(UpdateInformationRequest request);
        Task<AuthResult<string>> DeleteEmployeeAsync(string username);
        Task<AuthResult<CustomerDto>> UpdateCustomerInformationAsync(UpdateInformationRequest request);
        Task<AuthResult<string>> DeleteCustomerAsync(string phoneNumber);
    }
}