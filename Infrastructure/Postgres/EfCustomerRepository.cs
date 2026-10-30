using EasyCreditApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EasyCreditApi.Infrastructure.Postgres;

// Implementasi ICustomerRepository yang sama dengan OracleCustomerRepository, tapi lewat EF Core.
public sealed class EfCustomerRepository(AppDbContext db) : ICustomerRepository
{
    private const string UniqueViolation = "23505";   // PostgresErrorCodes.UniqueViolation

    public async Task<IEnumerable<Customer>> GetAllAsync(string? keyword)
        => await Filter(keyword).AsNoTracking().OrderBy(c => c.CustomerId).ToListAsync();

    public async Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(string? keyword, int page, int limit)
    {
        page = Math.Max(page, 1);
        limit = Math.Clamp(limit, 1, 100);

        var query = Filter(keyword).AsNoTracking();
        var total = await query.CountAsync();
        var items = await query.OrderBy(c => c.CustomerId).Skip((page - 1) * limit).Take(limit).ToListAsync();
        return (items, total);
    }

    public Task<Customer?> GetByIdAsync(int customerId)
        => db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == customerId);

    public async Task<IEnumerable<Customer>> GetByIdsAsync(IEnumerable<int> customerIds)
    {
        var ids = customerIds.Distinct().ToArray();

        if (ids.Length == 0)
            return Array.Empty<Customer>();

        if (ids.Length > 1000)
            throw new ArgumentException("Maksimal 1000 ID per query; pecah per batch.");

        return await db.Customers.AsNoTracking()
            .Where(c => ids.Contains(c.CustomerId))
            .OrderBy(c => c.CustomerId)
            .ToListAsync();
    }

    public Task<bool> ExistsAsync(int customerId)
        => db.Customers.AnyAsync(c => c.CustomerId == customerId);

    public Task<bool> NikExistsAsync(string nik, int? excludeCustomerId = null)
        => db.Customers.AnyAsync(c => c.Nik == nik
                                      && (excludeCustomerId == null || c.CustomerId != excludeCustomerId));

    public async Task<int> CreateAsync(Customer customer)
    {
        db.Customers.Add(customer);

        try
        {
            await db.SaveChangesAsync();   // mengisi CustomerId (identity)
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolation })
        {
            throw new DuplicateKeyException("NIK sudah terdaftar.", ex);
        }

        return customer.CustomerId;
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        try
        {
            // ExecuteUpdate langsung jadi satu UPDATE SQL, tanpa change tracking.
            return await db.Customers
                .Where(c => c.CustomerId == customer.CustomerId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Name, customer.Name)
                    .SetProperty(c => c.Nik, customer.Nik)
                    .SetProperty(c => c.Phone, customer.Phone)
                    .SetProperty(c => c.Email, customer.Email)
                    .SetProperty(c => c.Address, customer.Address)) > 0;
        }
        catch (PostgresException ex) when (ex.SqlState == UniqueViolation)
        {
            throw new DuplicateKeyException("NIK sudah dipakai customer lain.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int customerId)
        => await db.Customers.Where(c => c.CustomerId == customerId).ExecuteDeleteAsync() > 0;

    private IQueryable<Customer> Filter(string? keyword)
    {
        var q = db.Customers.AsQueryable();
        if (string.IsNullOrWhiteSpace(keyword)) return q;

        var pattern = "%" + keyword.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
        // LIKE case-insensitive (Npgsql). Escape char harus disebut eksplisit:
        // tanpa argumen ke-3 Npgsql membuat "ESCAPE ''" sehingga \% dan \_ tidak dianggap escape.
        return q.Where(c => EF.Functions.ILike(c.Name, pattern, @"\"));
    }
}
