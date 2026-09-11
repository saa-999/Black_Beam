using BlackBeam.Services.Loyalty.Data;
using BlackBeam.Services.Loyalty.Model;
using Microsoft.EntityFrameworkCore;
using BlackBeam.Services.Loyalty.Services;
using DotNetEnv;
Env.Load();

var builder = WebApplication.CreateBuilder(args);



var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];


builder.Services.AddDbContext<LoyalityDbContext>(opt =>
{
    opt.UseMySql(connectionString, new MySqlServerVersion(new Version(8,0,30)));
});

builder.Services.AddSignalR();


var app = builder.Build();

app.MapHub<CashierHub>("/CashierHub-Points");

app.Run();
