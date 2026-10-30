using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public interface ICreditApplicationRepository
{
    Task<(IEnumerable<CreditApplication> Items, int Total)> GetPagedAsync(
        int? customerId, string? status, int page, int limit);
    Task<CreditApplication?> GetByIdAsync(int applicationId);
    Task<int> CreateAsync(CreditApplication application);

    // Semua perubahan status di-guard di SQL (WHERE ... AND STATUS = ...);
    // return false kalau tidak ada baris yang berubah.
    Task<bool> UpdateDraftAsync(CreditApplication application);
    Task<bool> DeleteDraftAsync(int applicationId);
    Task<bool> SubmitAsync(int applicationId, int? creditScore);
    Task<bool> ApproveAsync(int applicationId, string approvedBy, IReadOnlyList<CreditInstallment> installments);
    Task<bool> RejectAsync(int applicationId, string rejectedBy);

    Task<IEnumerable<CreditInstallment>> GetInstallmentsAsync(int applicationId);
}
