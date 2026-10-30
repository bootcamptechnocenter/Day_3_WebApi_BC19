using EasyCreditApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EasyCreditApi.Infrastructure.Postgres;

// Contoh referensi EF Core + PostgreSQL. Hanya dipakai kalau Database:Provider = Postgres.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
