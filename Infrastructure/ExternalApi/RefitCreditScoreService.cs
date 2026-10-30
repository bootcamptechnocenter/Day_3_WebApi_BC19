using EasyCreditApi.Application.DTO.CreditScore;
using EasyCreditApi.Application.Interfaces;
using Refit;

namespace EasyCreditApi.Infrastructure.ExternalApi;

// Membungkus client Refit dengan graceful degradation yang sama seperti CreditScoreService.
public class RefitCreditScoreService : ICreditScoreService
{
    private readonly ICreditScoreApi _api;
    private readonly ILogger<RefitCreditScoreService> _logger;

    public RefitCreditScoreService(ICreditScoreApi api, ILogger<RefitCreditScoreService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ResCreditScoreDto?> GetScoreAsync(int customerId)
    {
        try
        {
            return await _api.GetScoreAsync(customerId);
        }
        catch (ApiException ex)   // response non-2xx atau body tidak valid
        {
            _logger.LogWarning(ex, "Credit score API (Refit) gagal untuk customer {CustomerId} (status {StatusCode})",
                customerId, ex.StatusCode);
            return null;
        }
        catch (ApiRequestException ex)   // Refit 16: request gagal terkirim (koneksi/timeout), aslinya di InnerException
        {
            _logger.LogWarning(ex, "Credit score API (Refit) tidak bisa dihubungi untuk customer {CustomerId}: {Reason}",
                customerId, ex.InnerException?.GetType().Name);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Credit score API (Refit) tidak bisa dihubungi untuk customer {CustomerId}", customerId);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Credit score API (Refit) timeout untuk customer {CustomerId}", customerId);
            return null;
        }
    }
}
