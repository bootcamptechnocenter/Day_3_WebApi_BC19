using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));



// ── DEPENDENCY INJECTION ──
// Singleton: agar data in-memory (List<Customer>) tetap ada antar-request.
// AddScoped akan membuat list baru tiap request sehingga data hilang setelah response.
builder.Services.AddSingleton<ICustomerService, CustomerService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
