using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Domain.Entities;

public class CustomerService : ICustomerService
{
    private readonly List<Customer> _customers = new();
    private int _nextId = 1;

    public Task<List<Customer>> GetAllAsync() => Task.FromResult(_customers.ToList());

    public Task<Customer?> GetByIdAsync(int id) =>
        Task.FromResult(_customers.FirstOrDefault(c => c.CustomerId == id));

    public Task<Customer> CreateAsync(ReqCreateCustomerDto dto)
    {
        var customer = new Customer
        {
            CustomerId = _nextId++,
            Name = dto.Name,
            Nik = dto.Nik,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _customers.Add(customer);
        return Task.FromResult(customer);
    }

    public async Task<Customer?> UpdateAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await GetByIdAsync(id);
        if (customer is null) return null;

        customer.Name = dto.Name;
        customer.Email = dto.Email;
        customer.MonthlyIncome = dto.MonthlyIncome;
        return customer;
    }

    // public Task<bool> DeleteAsync(int id) =>
    //     Task.FromResult(_customers.RemoveAll(c => c.Id == id) > 0);
}
