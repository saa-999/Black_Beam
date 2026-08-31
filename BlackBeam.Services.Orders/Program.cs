using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Orders.Data;
using DotNetEnv;
using Stripe;
using BlackBeam.Services.Orders.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

Env.Load();
var builder = WebApplication.CreateBuilder(args);
string? connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];
string? jwtSecret = builder.Configuration["JwtSettings:Secret"];
string? jwtIssuer = builder.Configuration["JwtSettings:Issuer"];

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(jwtSecret) || string.IsNullOrEmpty(jwtIssuer))
{
    throw new InvalidOperationException("An error occurred while retrieving environment variables");
}

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.RequireHttpsMetadata = false; // just in development 
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

    opt.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/CashierHub", StringComparison.OrdinalIgnoreCase)))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});




builder.Services.AddDbContext<OrderDbContext>(opt =>
{
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddHttpClient("InventoryClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5123");
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<OrderTrackingManager>();
builder.Services.AddHttpContextAccessor();


var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<CashierHub>("/CashierHub");

app.Run();
