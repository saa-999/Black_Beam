using BlackBeam.Services.Identity.Logg;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BlackBeam.Shared.Responses;
using BlackBeam.Services.Identity.Data;
using Microsoft.EntityFrameworkCore;
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Get Keys from .env file
string? jwtSecret = builder.Configuration["JwtSettings:Secret"];
string? jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
string? connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];

if (string.IsNullOrEmpty(jwtSecret) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(connectionString) )
{
    throw new InvalidOperationException("المتغيرات فراغة في ملف البيئة !!!");
}

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).

AddJwtBearer(opt =>
{
    opt.RequireHttpsMetadata = false;
    opt.SaveToken = true;

    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    opt.Events = new JwtBearerEvents {
        OnAuthenticationFailed = JwtSecurityEventsHandler.HandleAuthenticationFailed
    };
});
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();




app.Run();

