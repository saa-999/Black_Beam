using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BlackBeam.Services.Identity.Endpoints;
using BlackBeam.Services.Identity.Data;
using  BlackBeam.Services.Identity.Logg;
using BlackBeam.Services.Identity.Security;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Get Keys from .env file
string? jwtSecret = builder.Configuration["JwtSettings:Secret"];
string? jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
string? connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];


if (string.IsNullOrEmpty(jwtSecret) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(connectionString))
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
    opt.RequireHttpsMetadata = false; // just in development, in production should be true
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
        OnAuthenticationFailed = JwtSecurityEventsHandler.HandleAuthenticationFailed
    };
});
builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IHashService, HashService>();

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();



app.Run();

