using EasyCreditApi.Domain.Entities;

namespace EasyCreditApi.Application.Interfaces;
public interface IJwtTokenGenerator
{
    string GenerateToken(
        int userId,
        string name,
        string role);
}
