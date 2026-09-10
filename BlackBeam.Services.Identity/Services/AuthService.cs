using BlackBeam.Services.Identity.Data;
using BlackBeam.Services.Identity.Entities;
using BlackBeam.Services.Identity.Endpoints;
using BlackBeam.Services.Identity.Models;
using BlackBeam.Services.Identity.Security;
using BlackBeam.Shared.EnumRole;
using BlackBeam.Services.Identity.DTOs;
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

        public async Task<AuthResult<string>> LoginAsync(LoginRequest request)
        {
            string identifier = request.Identifier?.Trim() ?? string.Empty;
            string password = request.Password;

            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
            {
                return new AuthResult<string>(false, null, "بيانات الدخول غير مكتملة!!");
            }

            var user = await _db.UsersDB
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
            (u.Role == EnumRole.Customer && u.PhoneNumber == identifier) ||
            (u.Role != EnumRole.Customer && u.Username == identifier)
        );

            if (user == null)
            {
                return new AuthResult<string>(false, null, "البيانات المدخلة غير صحيحة!!");
            }

            if (!user.IsActive)
            {
                return new AuthResult<string>(false, null, "هذا الحساب غير نشط، يرجى مراجعة الإدارة!!");
            }



            bool isPasswordValid = _hashService.VerifyPassword(password, user.Password);

            if (!isPasswordValid)
            {
                return new AuthResult<string>(false, null, "كلمة المرور أو رقم الهاتف غير صحيح !");
            }


            string token = GenerateJwtToken(user);
            return new AuthResult<string>(true, token, null);

        }

        public async Task<AuthResult<string>> RegisterCustomerAsync(RegistRequest request)
        {
            string phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
            string name = request.Name?.Trim() ?? string.Empty;
            string password = request.Password;

            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(name))
                return new AuthResult<string>(false, null, "جميع الحقول (الاسم، الهاتف، كلمة المرور) إلزامية");


            string hashedPassword = _hashService.HashPassword(password);

            if (string.IsNullOrEmpty(hashedPassword))
                return new AuthResult<string>(false, null, "فشل في تشفير كلمة المرور");

            var phoneExists = await _db.UsersDB
            .AnyAsync(u => u.PhoneNumber == phoneNumber);
            if (phoneExists)
            {
                return new AuthResult<string>(false, null, "رقم الهاتف مسجل مسبقاً في النظام!!");
            }

            var user = new ApplicationUser
            {
                Name = name,
                PhoneNumber = phoneNumber,
                Password = hashedPassword,
                Role = EnumRole.Customer
            };

            _db.UsersDB.Add(user);
            await _db.SaveChangesAsync();
            return new AuthResult<string>(true, null, null);
        }
        public async Task<AuthResult<string>> RegisterCashierAsync(RegisterStaffRequest request)
        {
            string username = request.Username?.Trim() ?? string.Empty;
            string name = request.Name?.Trim() ?? string.Empty;
            string phoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
            string password = request.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(name))
            {
                return new AuthResult<string>(false, null, "اسم المستخدم، الاسم،   وكلمة المرور حقول إلزامية");
            }

            if (await _db.UsersDB
           .AnyAsync(u => u.Username == username ||
            u.PhoneNumber != string.Empty && u.PhoneNumber == phoneNumber))
            {
                return new AuthResult<string>(false, null, "اسم المستخدم أو رقم الهاتف مسجل مسبقاً في النظام!!");
            }

            string hashedPassword = _hashService.HashPassword(password);

            if (string.IsNullOrEmpty(hashedPassword))
            {
                return new AuthResult<string>(false, null, "فشل في تشفير كلمة المرور");
            }

            var user = new ApplicationUser
            {
                Name = name,
                Username = username,
                PhoneNumber = phoneNumber,
                Password = hashedPassword,
                Role = EnumRole.Cashier,
                IsActive = true
            };

            _db.UsersDB.Add(user);
            await _db.SaveChangesAsync();
            return new AuthResult<string>(true, null, null);
        }

        public async Task<AuthResult<IEnumerable<EmployeeDto>>> GetAllEmployeesAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize > 100 ? 100 : (pageSize < 1 ? 10 : pageSize);
            int skip = (pageNumber - 1) * pageSize;

            var employees = await _db.UsersDB
            .AsNoTracking()
            .Where(u => u.Role != EnumRole.Customer)
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(u => new EmployeeDto
            {
                Username = u.Username ?? string.Empty,
                Name = u.Name ?? string.Empty,
                PhoneNumber = u.PhoneNumber ?? string.Empty,
                Role = u.Role ?? string.Empty,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToListAsync();

            return new AuthResult<IEnumerable<EmployeeDto>>(true, employees, null);
        }

        public async Task<AuthResult<EmployeeDto>> UpdateEmployeeInformationAsync(UpdateInformationRequest request)
        {
            string? username = request.Username?.Trim();
            string? newUsername = request.NewUsername?.Trim();
            string? name = request.Name?.Trim();
            string? phoneNumber = request.PhoneNumber?.Trim();
            string? newPhoneNumber = request.NewPhoneNumber?.Trim();
            string? password = request.Password;
            string? newPassword = request.NewPassword;
            bool? isActive = request.IsActive;

            if (string.IsNullOrEmpty(username))
            {
                return new AuthResult<EmployeeDto>(false, null, "اسم المستخدم الحالي مطلوب.");
            }

            var user = await _db.UsersDB.FirstOrDefaultAsync(u => u.Username == username && u.Role != EnumRole.Customer);
            if (user == null)
            {
                return new AuthResult<EmployeeDto>(false, null, "المستخدم غير موجود.");
            }

            if (!string.IsNullOrEmpty(newUsername) && newUsername != username)
            {
                if (await _db.UsersDB.AnyAsync(u => u.Username == newUsername))
                {
                    return new AuthResult<EmployeeDto>(false, null, "اسم المستخدم الجديد مسجل مسبقاً.");
                }
                user.Username = newUsername;
            }

            user.Name = name ?? user.Name;

            if (!string.IsNullOrEmpty(newPhoneNumber))
            {
                if (await _db.UsersDB.AnyAsync(u => u.PhoneNumber == newPhoneNumber && u.Id != user.Id))
                {
                    return new AuthResult<EmployeeDto>(false, null, "رقم الهاتف الجديد مسجل مسبقاً.");
                }
                user.PhoneNumber = newPhoneNumber;
            }

            if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(newPassword))
            {
                if (!_hashService.VerifyPassword(password, user.Password))
                {
                    return new AuthResult<EmployeeDto>(false, null, "كلمة المرور الحالية غير صحيحة.");
                }
                user.Password = _hashService.HashPassword(newPassword);
            }

            user.IsActive = isActive.HasValue ? isActive.Value : user.IsActive;

            await _db.SaveChangesAsync();
            var updatedEmployee = new EmployeeDto
            {
                Username = user.Username ?? string.Empty,
                Name = user.Name ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = user.Role ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
            return new AuthResult<EmployeeDto>(true, updatedEmployee, null);
        }
        public async Task<AuthResult<string>> DeleteEmployeeAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new AuthResult<string>(false, null, "اسم المستخدم مطلوب.");
            }
            username = username.Trim();
            var user = await _db.UsersDB.FirstOrDefaultAsync(u => u.Username == username && u.Role != EnumRole.Customer);
            if (user == null)
            {
                return new AuthResult<string>(false, null, "المستخدم غير موجود.");
            }
            user.IsActive = false;
            await _db.SaveChangesAsync();
            return new AuthResult<string>(true, "تم حذف المستخدم بنجاح.", null);
        }
        public async Task<AuthResult<CustomerDto>> UpdateCustomerInformationAsync(UpdateInformationRequest request)
        {
            string? name = request.Name?.Trim();
            string? phoneNumber = request.PhoneNumber?.Trim();
            string? newPhoneNumber = request.NewPhoneNumber?.Trim();
            string? password = request.Password;
            string? newPassword = request.NewPassword;
            bool? isActive = request.IsActive;

            if (string.IsNullOrEmpty(phoneNumber))
            {
                return new AuthResult<CustomerDto>(false, null, "رقم الهاتف مطلوب.");
            }

            var user = await _db.UsersDB.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.Role == EnumRole.Customer);
            if (user == null)
            {
                return new AuthResult<CustomerDto>(false, null, "المستخدم غير موجود.");
            }

            user.Name = name ?? user.Name;

            if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(newPassword))
            {
                if (!_hashService.VerifyPassword(password, user.Password))
                {
                    return new AuthResult<CustomerDto>(false, null, "كلمة المرور الحالية غير صحيحة.");
                }
                user.Password = _hashService.HashPassword(newPassword);
            }

            if (isActive.HasValue)
            {
                user.IsActive = isActive.Value;
            }

            if (!string.IsNullOrEmpty(newPhoneNumber) && newPhoneNumber != phoneNumber)
            {
                if (await _db.UsersDB.AnyAsync(u => u.PhoneNumber == newPhoneNumber && u.Id != user.Id))
                {
                    return new AuthResult<CustomerDto>(false, null, "رقم الهاتف الجديد مسجل مسبقاً.");
                }
                user.PhoneNumber = newPhoneNumber;
            }

            await _db.SaveChangesAsync();

            return new AuthResult<CustomerDto>(true, new CustomerDto
            {
                FullName = user.Name ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Role = user.Role ?? string.Empty,
                CreatedAt = user.CreatedAt
            }, null);
        }
        public async Task<AuthResult<string>> DeleteCustomerAsync(string phoneNumber)
        {
            phoneNumber = phoneNumber.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return new AuthResult<string>(false, null, "رقم الجوال مطلوب");
            }

            var user = await _db.UsersDB.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
            if (user == null) return new AuthResult<string>(false, null, "رقم الهاتف غير صحيح !1");

            user.IsActive = false;
            await _db.SaveChangesAsync();

            return new AuthResult<string>(true, "تم تعطيل المستخدم", null);
        }
        private string GenerateJwtToken(ApplicationUser user)
        {
            string? secretKey = _config["JwtSettings:Secret"];
            string? issuer = _config["JwtSettings:Issuer"];
            string? audience = _config["JwtSettings:Audience"];

            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new InvalidOperationException("JWT configuration settings are missing or empty in the environment file!");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber  ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                new Claim(ClaimTypes.Upn, user.Username ?? string.Empty),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}