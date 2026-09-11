using BlackBeam.Services.Loyalty.Data;
using BlackBeam.Services.Loyalty.Endpoints;
using BlackBeam.Services.Loyalty.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BlackBeam.Shared.EnumRole;
using DotNetEnv;
Env.Load();

var builder = WebApplication.CreateBuilder(args);

string? connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];
string? jwtSecret = builder.Configuration["JwtSettings:Secret"];
string? jwtIssuer = builder.Configuration["JwtSettings:Issuer"];

if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(jwtSecret) || string.IsNullOrEmpty(jwtIssuer))
{
    throw new InvalidOperationException("المتغيرات فراغة في ملف البيئة !!!");
}

builder.Services.AddDbContext<LoyalityDbContext>(opt =>
{
    opt.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 30)));
});

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.RequireHttpsMetadata = false;
    opt.SaveToken = true;

    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    opt.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/CashierHub-Points", StringComparison.OrdinalIgnoreCase))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CashierOnly", policy => policy.RequireRole(EnumRole.Cashier));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(EnumRole.Admin));
    options.AddPolicy("CashierOrAdmin", policy => policy.RequireRole(EnumRole.Cashier, EnumRole.Admin));
});

builder.Services.AddSignalR();

builder.Services.AddHttpClient("Identity", client =>
{
    client.BaseAddress = new Uri("http://localhost:5264");
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<CashierHub>("/CashierHub-Points");
app.MapLoyaltyPointEndpoints();

app.Run();
