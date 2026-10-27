using EasyCreditApi.Application.DTO.Customer;
using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int customerId);
    Task<Customer?> CreateAsync(ReqCreateCustomerDto dto);
}
