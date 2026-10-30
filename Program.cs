using System.Text;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Infrastructure.Security;
using EasyCreditApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using EasyCreditApi.Application.Services;
using EasyCreditApi.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;
using EasyCreditApi.Infrastructure.ExternalApi;
using EasyCreditApi.Infrastructure.Oracle;
using EasyCreditApi.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Dapper;
using EasyCreditApi.Application.DTO;
using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Secret lokal (connection string, API key, JWT key) — file ini di-gitignore.
// Alternatif: environment variable (ConnectionStrings__Oracle, ConnectionStrings__Postgres, ApiKey, Jwt__Key)
// atau dotnet user-secrets.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Serilog: sink & level diatur di section "Serilog" pada appsettings.json.
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Tombol "Authorize" di /swagger: isi X-Api-Key dan token JWT (tanpa kata "Bearer").
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Description = "API key untuk semua endpoint /api/*"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "accessToken dari POST /api/auth/login"
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("ApiKey", document)] = [],
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

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

        // 401/403 dari JWT juga memakai format ApiResponse.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Unauthorized: token tidak valid atau tidak ada."));
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Forbidden: akses ditolak."));
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanApproveLoans", policy => policy.RequireRole("Admin"));
});

// ── DEPENDENCY INJECTION ──
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddCreditScoreService(builder.Configuration);

// Provider database untuk customers: Oracle (default, Dapper) atau Postgres (contoh EF Core).
// Applications (CREDIT_APPLICATIONS) hanya ada di Oracle.
var provider = builder.Configuration["Database:Provider"] ?? "Oracle";
var useOracle = !provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase);

if (useOracle)
{
    builder.Services.AddSingleton<OracleConnectionFactory>();
    builder.Services.AddScoped<ICustomerRepository, OracleCustomerRepository>();
    builder.Services.AddScoped<ICreditApplicationRepository, OracleCreditApplicationRepository>();
    builder.Services.AddScoped<ICreditApplicationService, CreditApplicationService>();
    builder.Services.AddScoped<OracleTransactionDemo>();
}
else
{
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
    builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
}

builder.Services.AddControllers()
    .ConfigureApplicationPartManager(parts =>
    {
        // Applications hanya ada di Oracle: di mode Postgres controller-nya tidak dipetakan (404).
        if (!useOracle)
            parts.FeatureProviders.Add(new ExcludeControllersFeatureProvider(typeof(ApplicationsController)));
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Error validasi model -> 400 dengan format ApiResponse.
        options.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

            return new BadRequestObjectResult(
                ApiResponse<Dictionary<string, string[]>>.Fail("Validasi gagal.", errors));
        };
    });
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

}

// Endpoint lab: hanya aktif di Development dan provider Oracle.
if (app.Environment.IsDevelopment() && useOracle)
{
    app.MapGet("/_dbping", async (OracleConnectionFactory factory) =>
    {
        await using var conn = factory.Create();
        var ok = await conn.ExecuteScalarAsync<int>("SELECT 1 FROM dual");

        return ok == 1
            ? Results.Ok(ApiResponse<object>.Success(new { database = "oracle", schema = "EASYCREDIT", ok = true }))
            : Results.Json(ApiResponse<object>.Fail("Oracle tidak merespons dengan benar."), statusCode: 500);
    });

    app.MapPost("/_tx-demo", async (OracleTransactionDemo demo, bool fail = true) =>
    {
        try
        {
            var id = await demo.InsertTwoAsync(fail);
            return Results.Ok(ApiResponse<object>.Success(new { committed = true, firstId = id }));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<object>.Fail(ex.Message), statusCode: 500);
        }
    });
}

// Urutan: Serilog request log (paling luar) -> correlation/request log -> exception -> API key.
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        if (http.Items.TryGetValue(RequestLoggingMiddleware.ItemKey, out var id))
            diag.Set(RequestLoggingMiddleware.ItemKey, id);
    };
});

app.UseRequestLogging();

app.UseCustomExceptionHandler();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Membuang controller tertentu dari daftar controller yang dipetakan MVC.
sealed class ExcludeControllersFeatureProvider(params Type[] excluded) : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var type in excluded)
            feature.Controllers.Remove(type.GetTypeInfo());
    }
}
