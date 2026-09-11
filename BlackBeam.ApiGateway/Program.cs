using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using DotNetEnv;
using System.Text.Json;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

string? jwtSecret = builder.Configuration["JwtSettings:Secret"];
string? jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
if (string.IsNullOrEmpty(jwtSecret) || string.IsNullOrEmpty(jwtIssuer))
{
     throw new InvalidOperationException("المتغيرات فراغة في ملف البيئة !!!");
}

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(opt => 
{
    opt.AddPolicy("AllowAll", b => b 
     .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()); 
});

builder.Services.AddRateLimiter(options =>
{
   options.AddFixedWindowLimiter("LoginPolicy", opt => 
   {
      opt.Window = TimeSpan.FromMinutes(1);
      opt.PermitLimit = 5;  
   });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), 
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false 
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuth", policy => policy.RequireAuthenticatedUser());
});



var app = builder.Build();

app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
