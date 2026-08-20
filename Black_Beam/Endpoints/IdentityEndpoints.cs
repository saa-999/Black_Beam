using BlackBeam.Services.Identity.Data;
using BlackBeam.Services.Identity.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using BlackBeam.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BlackBeam.Services.Identity.Endpoints
{
    public static class IdentityEndpoints
    {
        public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth");

            group.MapPost("/login", async(LoginRequest req, IConfiguration config, AppDbContext db) =>
            {
                if(string.IsNullOrEmpty(req.PhoneNumber) || string.IsNullOrEmpty(req.Password))
                {
                    return Results.BadRequest(ApiResponse<string>.Failure(new List<string> { "رقم الهاتف أو كلمة المرور فارغة" }, "فشل تسجيل الدخول"));
                }

                var user = await db.UsersDB.FirstOrDefaultAsync(u => u.PhoneNumber == req.PhoneNumber && u.Password == req.Password);

                if (user == null)
                {
                    return Results.Unauthorized();
                }


                if(!user.IsActive)
                {
                    return Results.Unauthorized();
                }

                try
                {
                    string token = GenerateJwtToken(user, config);
                    return Results.Ok(ApiResponse<string>.Success(token, "تم تسجيل الدخول بنجاح"));
                }
                catch (InvalidOperationException)
                {
                    return Results.Unauthorized();

                }
            });
               
          
        }

        private static string GenerateJwtToken(ApplicationUser user, IConfiguration config)
        { 
            string? secretKey = config["JwtSettings:Secret"];
            string? issuer = config["JwtSettings:Issuer"];
            string? audience = config["JwtSettings:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("المتغيرات فراغة في ملف البيئة !!!");
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub , user.Id.ToString() ),
                new Claim(ClaimTypes.MobilePhone , user.PhoneNumber),
                new Claim(ClaimTypes.Role  , user.Role),
                new Claim(JwtRegisteredClaimNames.Jti , Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                signingCredentials: creds,
                 claims: claims,
                 expires: DateTime.UtcNow.AddHours(8));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



    }
    public record LoginRequest(string PhoneNumber, string Password);
}
