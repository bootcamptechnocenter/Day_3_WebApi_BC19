using EasyCreditApi.Application.DTO.CreditScore;

namespace EasyCreditApi.Application.Interfaces;

public interface ICreditScoreService
{
    // null = bureau tidak bisa dihubungi / error; pemanggil harus tetap jalan.
    Task<ResCreditScoreDto?> GetScoreAsync(int customerId);
}
