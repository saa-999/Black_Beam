using BlackBeam.Services.Identity.Services;
using BlackBeam.Shared.Responses;

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
                return Results.Ok(ApiResponse<string>.Success(result.Token!, "تم تسجيل الدخول بنجاح"));
            });

            group.MapPost("/Regist", async (RegistRequest req, IAuthService authService) =>
            {
                var result = await authService.RegisterCustomerAsync(req);
                if (!result.IsSuccess)
                {
                    return Results.BadRequest(ApiResponse<string>.Failure(new List<string> { result.ErrorMessage ?? "فشل التسجيل" }));
                }
                return Results.Ok(ApiResponse<string>.Success("تم تسجيل المستخدم بنجاح"));
            });

            
        }
     public record LoginRequest(string PhoneNumber, string Password);
    public record RegistRequest(string PhoneNumber , string Password   , string Name );
    }
}