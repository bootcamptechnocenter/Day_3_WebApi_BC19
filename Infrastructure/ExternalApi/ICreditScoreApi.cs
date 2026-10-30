using EasyCreditApi.Application.DTO.CreditScore;
using Refit;

namespace EasyCreditApi.Infrastructure.ExternalApi;

// Varian Refit: implementasi HTTP dibuat otomatis dari interface ini.
public interface ICreditScoreApi
{
    [Get("/api/v1/score/{customerId}")]
    Task<ResCreditScoreDto> GetScoreAsync(int customerId);
}
