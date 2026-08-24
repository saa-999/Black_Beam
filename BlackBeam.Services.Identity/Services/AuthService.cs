using BlackBeam.Services.Identity.Data;
using BlackBeam.Services.Identity.Entities;
using BlackBeam.Services.Identity.Endpoints;
using BlackBeam.Services.Identity.Models;
using BlackBeam.Services.Identity.Security;
using BlackBeam.Shared.EnumRole;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlackBeam.Services.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IHashService _hashService;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IHashService hashService, IConfiguration config)
        {
            _db = db;
            _hashService = hashService;
            _config = config;
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Password))
            {
                return new AuthResult(false, null, "رقم الهاتف أو كلمة المرور فارغة");
            }

            var user = await _db.UsersDB.FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber);
            
            if (user == null)
            {
                return new AuthResult(false, null, "كلمة المرور أو رقم الهاتف غير صحيح، أو الحساب غير نشط");
            }

            if (!user.IsActive)
            {
                return new AuthResult(false, null, "الحساب غير نشط");
            }

            var hash = new HashPassword
            {
                Raw = request.Password,
                Hash = user.Password
            };
            
            _hashService.VerifyPassword(hash);

            if (!hash.IsSucceeded)
            {
                return new AuthResult(false, null, "كلمة المرور أو رقم الهاتف غير صحيح !");
            }

            try
            {
                string token = GenerateJwtToken(user);
                return new AuthResult(true, token, null);
            }
            catch (Exception)
            {
                return new AuthResult(false, null, "حدث خطأ داخلي أثناء إنشاء جلسة الدخول");
            }
        }

        public async Task<AuthResult> RegisterCustomerAsync(RegistRequest request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.Name))
                return new AuthResult(false, null, "جميع الحقول (الاسم، الهاتف، كلمة المرور) إلزامية");

            var hash = new HashPassword { Raw = request.Password };
            _hashService.HashPassword(hash);

            if (!hash.IsSucceeded)
                return new AuthResult(false, null, "فشل في تشفير كلمة المرور");

            var user = new ApplicationUser
            {
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Password = hash.Hash,
                Role = EnumRole.Customer 
            };

            try
            {
                await _db.UsersDB.AddAsync(user);
                await _db.SaveChangesAsync();
                return new AuthResult(true, null, null);
            }
            catch (DbUpdateException)
            {
                return new AuthResult(false, null, "البيانات المدخلة تخالف شروط النظام (مثال: رقم هاتف غير صالح).");
            }
        }

     
        private string GenerateJwtToken(ApplicationUser user)
        { 
            string? secretKey = _config["JwtSettings:Secret"];
            string? issuer = _config["JwtSettings:Issuer"];
            string? audience = _config["JwtSettings:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("المتغيرات فارغة في ملف البيئة !!!");
            }
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}