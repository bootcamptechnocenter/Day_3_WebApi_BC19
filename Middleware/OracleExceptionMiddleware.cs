using EasyCreditApi.Application.DTO;
using EasyCreditApi.Infrastructure.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace EasyCreditApi.Middleware;

// Jaring pengaman untuk OracleException yang tidak ditangani repository/service:
// kode ORA dipetakan ke status HTTP + ApiResponse, detail asli hanya masuk log.
// Dipasang di dalam ExceptionMiddleware supaya OracleException ditangkap di sini lebih dulu.
public class OracleExceptionMiddleware
{
    // Rentang RAISE_APPLICATION_ERROR (-20000..-20999) dari PL/SQL / stored procedure kita sendiri.
    private const int UserErrorMin = 20000;
    private const int UserErrorMax = 20999;
    private const int RetryAfterSeconds = 5;

    private readonly RequestDelegate _next;
    private readonly ILogger<OracleExceptionMiddleware> _logger;

    public OracleExceptionMiddleware(RequestDelegate next, ILogger<OracleExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OracleException ex) when (!context.Response.HasStarted)
        {
            var (number, info, message) = Resolve(ex);

            _logger.Log(info.LogLevel, ex,
                "Oracle error ORA-{OracleErrorNumber:D5} on {Method} {Path} -> {StatusCode}",
                number, context.Request.Method, context.Request.Path.Value, info.StatusCode);

            context.Response.Clear();
            context.Response.StatusCode = info.StatusCode;
            if (info.Transient)
                context.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();

            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message));
        }
    }

    private static (int Number, OracleErrorInfo Info, string Message) Resolve(OracleException ex)
    {
        if (ex.Number is >= UserErrorMin and <= UserErrorMax)
        {
            // Pesan ditulis sendiri di PL/SQL, aman ditampilkan ke client.
            var info = new OracleErrorInfo(StatusCodes.Status409Conflict, "", LogLevel.Warning);
            return (ex.Number, info, UserErrorText(ex));
        }

        // ex.Number = error teratas; ex.Errors bisa berisi penyebab yang lebih spesifik.
        var numbers = new List<int> { ex.Number };
        numbers.AddRange(ex.Errors.Cast<OracleError>().Select(e => e.Number));

        var (number, mapped) = OracleErrorMap.Resolve(numbers);
        return (number, mapped, mapped.Message);
    }

    // "ORA-20001: Pengajuan bukan SUBMITTED\nORA-06512: at ..." -> "Pengajuan bukan SUBMITTED"
    private static string UserErrorText(OracleException ex)
    {
        var firstLine = ex.Message.Split('\n', 2)[0].Trim();
        var prefix = $"ORA-{ex.Number:D5}:";
        return firstLine.StartsWith(prefix, StringComparison.Ordinal)
            ? firstLine[prefix.Length..].Trim()
            : firstLine;
    }
}

public static class OracleExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseOracleExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<OracleExceptionMiddleware>();
}
