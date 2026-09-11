using BlackBeam.Services.Identity.Services;
using BlackBeam.Shared.Responses;
using BlackBeam.Services.Identity.Models;
using BlackBeam.Services.Identity.Entities;
using BlackBeam.Services.Identity.Data;
using BlackBeam.Services.Identity.Security;
using BlackBeam.Shared.EnumRole;
using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Identity.DTOs;
using Superpower.Model;

namespace BlackBeam.Services.Identity.Endpoints
{
    public static class IdentityEndpoints
    {
        public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth");

            group.MapPost("/login", async (LoginRequest req, IAuthService authService) =>
            {
                var result = await authService.LoginAsync(req);
                if (!result.IsSuccess)
                {
                    return Results.BadRequest(ApiResponse<string>.Failure(new List<string> { result.ErrorMessage ?? "فشل تسجيل الدخول" }));
                }
                return Results.Ok(ApiResponse<string>.Success(result.Data!, "تم تسجيل الدخول بنجاح"));
            });

            group.MapPost("/Regist", async (RegistRequest req, IAuthService authService) =>
            {
                var result = await authService.RegisterCustomerAsync(req);
                if (!result.IsSuccess)
                {
                    return Results.BadRequest(ApiResponse<string>.Failure(new List<string> { result.ErrorMessage ?? "فشل التسجيل" }));
                }
                return Results.Ok(ApiResponse<string>.Success(result.Data!, "تم تسجيل المستخدم بنجاح"));
            });
            group.MapPost("/admin/register-cashier", async (RegisterStaffRequest req, IAuthService authService) =>
            {
                var result = await authService.RegisterCashierAsync(req);
    
                if (!result.IsSuccess)
                {
                   return Results.BadRequest(ApiResponse<string>.Failure(new List<string> { result.ErrorMessage ?? "فشل تسجيل الكاشير" }));
                }
    
                return Results.Ok(ApiResponse<string>.Success(result.Data!, "تم إنشاء حساب الكاشير بنجاح"));
            }) .RequireAuthorization("AdminOnly");

            
            group.MapGet("/admin/employees", async (int? pageNumber, int? pageSize, IAuthService authService) =>
            {
                var result = await authService.GetAllEmployeesAsync(pageNumber ?? 1 ,pageSize ?? 10 );
                if (!result.IsSuccess)
                {
                    return Results.BadRequest(ApiResponse<IEnumerable<EmployeeDto>>.Failure(new List<string> { result.ErrorMessage ?? "فشل في جلب الموظفين" }));
                }
                return Results.Ok(ApiResponse<IEnumerable<EmployeeDto>>.Success(result.Data!, "تم جلب الموظفين بنجاح"));
            }).RequireAuthorization("AdminOnly");

            group.MapPut("/admin/update-employee", async (UpdateInformationRequest request , IAuthService authService) =>
            {
                var result = await authService.UpdateEmployeeInformationAsync(request);
                if (!result.IsSuccess)
                {
                    return Results.BadRequest(ApiResponse<string>.Failure(new List<string> {result.ErrorMessage ?? "حدث خطاء"}));
                }
                return Results.Ok(ApiResponse<EmployeeDto>.Success(result.Data));
            }).RequireAuthorization("AdminOnly");

            group.MapDelete("/admin/delete-employee", async (string username , IAuthService authService) =>
            {
               var result = await authService.DeleteEmployeeAsync(username);
                if (!result.IsSuccess)
                {
                   return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "لم تتم عملية الحذف"]));
                }
                return Results.Ok(ApiResponse<string>.Success(result.Data));
            }).RequireAuthorization("AdminOnly");

            group.MapPut("/custome-update", async (UpdateInformationRequest request ,IAuthService authService ) =>
            {
               var result = await authService.UpdateCustomerInformationAsync(request);
               if(!result.IsSuccess) return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "لم تتم عملية الحذف"]));
               return Results.Ok(ApiResponse<CustomerDto>.Success(result.Data));
            });

            group.MapDelete("/custome-delete" , async (string phoneNumber, IAuthService authService) =>
            {
                var result =  await authService.DeleteCustomerAsync(phoneNumber);
                if(!result.IsSuccess) return Results.BadRequest(ApiResponse<string>.Failure([result.ErrorMessage ?? "لم تتم العملية"]));
                return Results.Ok(ApiResponse<string>.Success(result.Data ));
            }).RequireAuthorization("AdminOnly");
        }
    }
}