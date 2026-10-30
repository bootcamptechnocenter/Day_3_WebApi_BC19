using EasyCreditApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyCreditApi.Infrastructure.Postgres;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        // Bentuk sama dengan tabel Oracle EASYCREDIT.CUSTOMERS, gaya Postgres (snake_case).
        b.ToTable("customers", "easycredit");
        b.HasKey(c => c.CustomerId);
        b.Property(c => c.CustomerId).HasColumnName("id").ValueGeneratedOnAdd();   // identity column
        b.Property(c => c.Name).HasColumnName("cust_name").HasMaxLength(100).IsRequired();
        b.Property(c => c.Nik).HasColumnName("nik").HasMaxLength(20).IsRequired();
        b.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(20);
        b.Property(c => c.Email).HasColumnName("email").HasMaxLength(100);
        b.Property(c => c.Address).HasColumnName("address").HasMaxLength(100);
        // PENTING: Customer.CreatedAt default DateTime.Now (Kind=Local). Npgsql menolak menulis DateTime
        // non-UTC ke "timestamp with time zone", jadi pakai "timestamp" biasa.
        b.Property(c => c.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp")
            .HasDefaultValueSql("now()");
        b.HasIndex(c => c.Nik).IsUnique().HasDatabaseName("ux_customers_nik");
    }
}
