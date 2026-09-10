using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Inventory.Data;
using BlackBeam.Services.Inventory.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BlackBeam.Services.Inventory.Endpoints;
using BlackBeam.Shared.EnumRole;
Env.Load();
var builder = WebApplication.CreateBuilder(args);

string? connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"] 
    ?? throw new InvalidOperationException("رسالة الخطأ الخاصة بك هنا");


builder.Services.AddDbContext<InventoryDbContext>(opt =>
{
   opt.UseMySql(connectionString , ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = false,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]!))
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("Admin", policy =>
    {
        policy.RequireRole(EnumRole.Admin);
    });
    opt.AddPolicy("StaffOnly", policy =>
    {
        policy.RequireRole(EnumRole.Admin, EnumRole.Cashier);
    });
});

builder.Services.AddScoped<IInventoryServices, InventoryServices>();
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapInventoryEndpoints();

app.Run();
