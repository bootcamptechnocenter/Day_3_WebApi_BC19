using EasyCreditApi.Application.DTO.Application;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;

public interface ICreditApplicationService
{
    Task<(IReadOnlyList<CreditApplication> Items, int Total)> GetPagedAsync(
        int? customerId, string? status, int page, int size);
    Task<CreditApplication> GetByIdAsync(int id);
    Task<CreditApplication> CreateAsync(ReqCreateApplicationDto dto);
    Task<CreditApplication> UpdateAsync(int id, ReqUpdateApplicationDto dto);
    Task DeleteAsync(int id);
    Task<CreditApplication> SubmitAsync(int id);
    Task<CreditApplication> ApproveAsync(int id, string approvedBy);
    Task<CreditApplication> RejectAsync(int id, string rejectedBy);
    Task<IReadOnlyList<CreditInstallment>> GetInstallmentsAsync(int id);
}
