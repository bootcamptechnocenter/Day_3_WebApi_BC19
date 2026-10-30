using EasyCreditApi.Application.DTO.Application;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;

namespace EasyCreditApi.Application.Services;

public sealed class CreditApplicationService : ICreditApplicationService
{
    private readonly ICreditApplicationRepository _repository;
    private readonly ICustomerRepository _customers;
    private readonly ICreditScoreService _creditScore;
    private readonly ILogger<CreditApplicationService> _logger;

    public CreditApplicationService(
        ICreditApplicationRepository repository,
        ICustomerRepository customers,
        ICreditScoreService creditScore,
        ILogger<CreditApplicationService> logger)
    {
        _repository = repository;
        _customers = customers;
        _creditScore = creditScore;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<CreditApplication> Items, int Total)> GetPagedAsync(
        int? customerId, string? status, int page, int size)
    {
        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            normalizedStatus = status.Trim().ToUpperInvariant();
            if (!CreditApplicationStatus.All.Contains(normalizedStatus))
                throw new ExceptionMiddleware.ValidationException(
                    $"Status tidak dikenal. Pilihan: {string.Join(", ", CreditApplicationStatus.All)}.");
        }

        var (items, total) = await _repository.GetPagedAsync(customerId, normalizedStatus, page, size);
        return (items.ToList(), total);
    }

    public async Task<CreditApplication> GetByIdAsync(int id) =>
        await _repository.GetByIdAsync(id)
        ?? throw new ExceptionMiddleware.NotFoundException($"Pengajuan {id} tidak ditemukan.");

    public async Task<CreditApplication> CreateAsync(ReqCreateApplicationDto dto)
    {
        // customerId ada di body, bukan di URL -> 400 (bukan 404) kalau customer tidak ada.
        if (!await _customers.ExistsAsync(dto.CustomerId))
            throw new ExceptionMiddleware.ValidationException($"Customer {dto.CustomerId} tidak ditemukan.");

        var application = new CreditApplication
        {
            CustomerId = dto.CustomerId,
            Amount = dto.Amount,
            TenorMonths = dto.TenorMonths,
            InterestRate = dto.InterestRate,
            Purpose = dto.Purpose?.Trim()
        };

        await _repository.CreateAsync(application);
        return application;
    }

    public async Task<CreditApplication> UpdateAsync(int id, ReqUpdateApplicationDto dto)
    {
        var application = await GetByIdAsync(id);
        EnsureStatus(application, CreditApplicationStatus.Draft, "diubah");

        application.Amount = dto.Amount;
        application.TenorMonths = dto.TenorMonths;
        application.InterestRate = dto.InterestRate;
        application.Purpose = dto.Purpose?.Trim();

        if (!await _repository.UpdateDraftAsync(application))
            throw ChangedConcurrently(id);

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var application = await GetByIdAsync(id);
        EnsureStatus(application, CreditApplicationStatus.Draft, "dihapus");

        if (!await _repository.DeleteDraftAsync(id))
            throw ChangedConcurrently(id);
    }

    public async Task<CreditApplication> SubmitAsync(int id)
    {
        var application = await GetByIdAsync(id);
        EnsureStatus(application, CreditApplicationStatus.Draft, "di-submit");

        // Score null (bureau down) tidak menggagalkan submit; CREDIT_SCORE tetap NULL.
        var score = await _creditScore.GetScoreAsync(application.CustomerId);
        if (score is null)
            _logger.LogWarning("Credit score untuk customer {CustomerId} tidak tersedia; submit tanpa score.",
                application.CustomerId);

        if (!await _repository.SubmitAsync(id, score?.Score))
            throw ChangedConcurrently(id);

        return await GetByIdAsync(id);
    }

    public async Task<CreditApplication> ApproveAsync(int id, string approvedBy)
    {
        EnsureDecider(approvedBy);
        var application = await GetByIdAsync(id);
        EnsureStatus(application, CreditApplicationStatus.Submitted, "di-approve");

        var schedule = AnnuityCalculator.BuildSchedule(
            application.Amount, application.InterestRate, application.TenorMonths, DateTime.Today);

        if (!await _repository.ApproveAsync(id, approvedBy, schedule))
            throw ChangedConcurrently(id);

        return await GetByIdAsync(id);
    }

    public async Task<CreditApplication> RejectAsync(int id, string rejectedBy)
    {
        EnsureDecider(rejectedBy);
        var application = await GetByIdAsync(id);
        EnsureStatus(application, CreditApplicationStatus.Submitted, "di-reject");

        if (!await _repository.RejectAsync(id, rejectedBy))
            throw ChangedConcurrently(id);

        return await GetByIdAsync(id);
    }

    public async Task<IReadOnlyList<CreditInstallment>> GetInstallmentsAsync(int id)
    {
        await GetByIdAsync(id);   // 404 kalau pengajuan tidak ada
        return (await _repository.GetInstallmentsAsync(id)).ToList();
    }

    private static void EnsureStatus(CreditApplication application, string expected, string action)
    {
        if (application.Status != expected)
            throw new ExceptionMiddleware.ConflictException(
                $"Pengajuan {application.ApplicationId} berstatus {application.Status}; " +
                $"hanya pengajuan {expected} yang bisa {action}.");
    }

    private static void EnsureDecider(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ExceptionMiddleware.ValidationException("Nama user tidak ada di token (claim 'name').");
    }

    // Guarded UPDATE/DELETE mengubah 0 baris: status berubah di antara baca dan tulis.
    private static ExceptionMiddleware.ConflictException ChangedConcurrently(int id) =>
        new($"Status pengajuan {id} baru saja berubah; muat ulang dan coba lagi.");
}
