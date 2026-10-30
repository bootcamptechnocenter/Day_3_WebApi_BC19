using EasyCreditApi.Application.DTO.CreditScore;
using EasyCreditApi.Application.Interfaces;

namespace EasyCreditApi.Infrastructure.ExternalApi;

// Dipakai saat CreditScoreApi:BaseUrl kosong (bureau asli belum ada).
// Deterministik: customerId yang sama selalu dapat score yang sama (300-850).
public class FakeCreditScoreService : ICreditScoreService
{
    public Task<ResCreditScoreDto?> GetScoreAsync(int customerId)
    {
        var score = 300 + (int)((uint)customerId * 7919u % 551u);

        return Task.FromResult<ResCreditScoreDto?>(new ResCreditScoreDto
        {
            CustomerId = customerId,
            Score = score,
            Grade = ToGrade(score),
            Source = "FAKE"
        });
    }

    private static string ToGrade(int score) => score switch
    {
        >= 750 => "A",
        >= 650 => "B",
        >= 550 => "C",
        >= 450 => "D",
        _ => "E"
    };
}
