using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Infrastructure;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
}
