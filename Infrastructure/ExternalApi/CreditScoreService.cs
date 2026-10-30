using System.Text.Json;
using EasyCreditApi.Application.DTO.CreditScore;
using EasyCreditApi.Application.Interfaces;

namespace EasyCreditApi.Infrastructure.ExternalApi;

// Typed HttpClient: BaseAddress, timeout, dan header X-Api-Key diatur saat registrasi.
public class CreditScoreService : ICreditScoreService
{
    private readonly HttpClient _http;
    private readonly ILogger<CreditScoreService> _logger;

    public CreditScoreService(HttpClient http, ILogger<CreditScoreService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ResCreditScoreDto?> GetScoreAsync(int customerId)
    {
        try
        {
            // Non-2xx -> HttpRequestException (ditangkap di bawah).
            return await _http.GetFromJsonAsync<ResCreditScoreDto>($"/api/v1/score/{customerId}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Credit score API gagal untuk customer {CustomerId} (status {StatusCode})",
                customerId, ex.StatusCode);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Credit score API timeout untuk customer {CustomerId}", customerId);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Response credit score API tidak valid untuk customer {CustomerId}", customerId);
            return null;
        }
    }
}
