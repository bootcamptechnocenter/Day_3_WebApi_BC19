using EasyCreditApi.Application.DTO;
using EasyCreditApi.Application.DTO.Application;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyCreditApi.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly ICreditApplicationService _service;

    public ApplicationsController(ICreditApplicationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        page = Math.Max(page, 1);
        size = Math.Clamp(size, 1, 100);

        var (items, total) = await _service.GetPagedAsync(customerId, status, page, size);
        var data = new { page, size, total, items = items.Select(ToDto) };
        return Ok(ApiResponse<object>.Success(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var application = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<ResApplicationDto>.Success(ToDto(application)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ReqCreateApplicationDto dto)
    {
        var application = await _service.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = application.ApplicationId },
            ApiResponse<ResApplicationDto>.Success(ToDto(application), "Pengajuan berhasil dibuat."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ReqUpdateApplicationDto dto)
    {
        var application = await _service.UpdateAsync(id, dto);
        return Ok(ApiResponse<ResApplicationDto>.Success(ToDto(application), "Pengajuan berhasil diperbarui."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        var application = await _service.SubmitAsync(id);
        return Ok(ApiResponse<ResApplicationDto>.Success(ToDto(application), "Pengajuan berhasil di-submit."));
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = "CanApproveLoans")]
    public async Task<IActionResult> Approve(int id)
    {
        var application = await _service.ApproveAsync(id, User.Identity?.Name ?? string.Empty);
        return Ok(ApiResponse<ResApplicationDto>.Success(ToDto(application), "Pengajuan disetujui."));
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = "CanApproveLoans")]
    public async Task<IActionResult> Reject(int id)
    {
        var application = await _service.RejectAsync(id, User.Identity?.Name ?? string.Empty);
        return Ok(ApiResponse<ResApplicationDto>.Success(ToDto(application), "Pengajuan ditolak."));
    }

    [HttpGet("{id:int}/installments")]
    public async Task<IActionResult> GetInstallments(int id)
    {
        var installments = await _service.GetInstallmentsAsync(id);
        return Ok(ApiResponse<IEnumerable<ResInstallmentDto>>.Success(installments.Select(ToDto)));
    }

    private static ResApplicationDto ToDto(CreditApplication a) => new()
    {
        ApplicationId = a.ApplicationId,
        CustomerId = a.CustomerId,
        Amount = a.Amount,
        TenorMonths = a.TenorMonths,
        InterestRate = a.InterestRate,
        Purpose = a.Purpose,
        Status = a.Status,
        CreditScore = a.CreditScore,
        ApprovedBy = a.ApprovedBy,
        ApprovedAt = a.ApprovedAt,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };

    private static ResInstallmentDto ToDto(CreditInstallment i) => new()
    {
        InstallmentId = i.InstallmentId,
        InstallmentNo = i.InstallmentNo,
        DueDate = i.DueDate,
        Amount = i.Amount,
        Status = i.Status
    };
}
