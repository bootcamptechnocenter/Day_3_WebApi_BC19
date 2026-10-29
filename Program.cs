using System.Text;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Infrastructure.Security;
using EasyCreditApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt;
using EasyCreditApi.Application.Services;
using EasyCreditApi.Infrastructure.Oracle;
using Dapper;

var builder = WebApplication.CreateBuilder(args);

// Secret lokal (connection string, API key, JWT key) — file ini di-gitignore.
// Alternatif: environment variable (ConnectionStrings__Postgres, ApiKey, Jwt__Key).
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

/// JWT
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Konfigurasi 'Jwt' tidak ditemukan");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            RoleClaimType = "role",
            NameClaimType = "name",
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── DEPENDENCY INJECTION ──
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<OracleConnectionFactory>();
builder.Services.AddScoped<ICustomerRepository, OracleCustomerRepository>();
builder.Services.AddScoped<OracleTransactionDemo>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    // Endpoint lab: hanya aktif di Development.
    app.MapGet("/_dbping", async (OracleConnectionFactory factory) =>
    {
        await using var conn = factory.Create();
        var ok = await conn.ExecuteScalarAsync<int>("SELECT 1 FROM dual");

        return ok == 1
            ? Results.Ok(new { database = "oracle", schema = "EASYCREDIT", ok = true })
            : Results.Problem("Oracle tidak merespons dengan benar.");
    });

    app.MapPost("/_tx-demo", async (OracleTransactionDemo demo, bool fail = true) =>
    {
        try
        {
            var id = await demo.InsertTwoAsync(fail);
            return Results.Ok(new { committed = true, firstId = id });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    });
}

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.UseCustomExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();