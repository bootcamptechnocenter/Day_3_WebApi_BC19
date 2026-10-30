using EasyCreditApi.Application.DTO.CreditScore;
using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Middleware;

namespace EasyCreditApi.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly ICreditScoreService _creditScore;

    public CustomerService(ICustomerRepository repository, ICreditScoreService creditScore)
    {
        _repository = repository;
        _creditScore = creditScore;
    }

    public async Task<(IReadOnlyList<Customer> Items, int Total)> GetPagedAsync(
        string? keyword, int page, int size)
    {
        var (items, total) = await _repository.GetPagedAsync(keyword, page, size);
        return (items.ToList(), total);
    }

    public Task<Customer?> GetByIdAsync(int customerId) =>
        _repository.GetByIdAsync(customerId);

    public async Task<Customer> CreateAsync(ReqCreateCustomerDto dto)
    {
        var nik = dto.Nik.Trim();

        if (await _repository.NikExistsAsync(nik))
            throw new ExceptionMiddleware.ConflictException("NIK sudah terdaftar.");

        var customer = new Customer
        {
            Name = dto.Name.Trim(),
            Nik = nik,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address
        };

        try
        {
            await _repository.CreateAsync(customer);
        }
        catch (DuplicateKeyException)
        {
            // Race condition: NIK dipakai request lain di antara cek dan insert (unique index NIK).
            throw new ExceptionMiddleware.ConflictException("NIK sudah terdaftar.");
        }

        return customer;
    }

    public async Task<Customer?> UpdateAsync(int id, ReqUpdateCustomerDto dto)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer is null) return null;

        var nik = dto.Nik.Trim();

        if (await _repository.NikExistsAsync(nik, id))
            throw new ExceptionMiddleware.ConflictException("NIK sudah dipakai customer lain.");

        customer.Name = dto.Name.Trim();
        customer.Nik = nik;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.Address = dto.Address;

        try
        {
            var updated = await _repository.UpdateAsync(customer);
            return updated ? customer : null;
        }
        catch (DuplicateKeyException)
        {
            throw new ExceptionMiddleware.ConflictException("NIK sudah dipakai customer lain.");
        }
    }

    public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);

    // null = customer ada tapi bureau tidak bisa memberi score.
    public async Task<ResCreditScoreDto?> GetCreditScoreAsync(int customerId)
    {
        if (!await _repository.ExistsAsync(customerId))
            throw new ExceptionMiddleware.NotFoundException("Customer tidak ditemukan.");

        return await _creditScore.GetScoreAsync(customerId);
    }
}
