using EasyCreditApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyCreditApi.Infrastructure;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
}
