using Microsoft.EntityFrameworkCore;
using Thorns.Models;
using Thorns.Models.Enum;

namespace Thorns.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                .HasColumnName("id");

            entity.Property(u => u.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(u => u.Cpf)
                .HasColumnName("cpf")
                .HasMaxLength(11);

            entity.HasIndex(u => u.Cpf)
                .IsUnique()
                .HasFilter("\"cpf\" IS NOT NULL");

            entity.Property(u => u.Cep)
                .HasColumnName("cep")
                .HasMaxLength(8);

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired();

            entity.Property(u => u.TokenVersion)
                .HasColumnName("token_version")
                .HasDefaultValue(0);

            entity.Property(u => u.Role)
    .HasColumnName("role")
    .HasConversion<string>()
    .HasMaxLength(20)
    .HasDefaultValue(UserRole.User);

            entity.Property(u => u.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at");

            entity.Property(u => u.LastLogin)
                .HasColumnName("last_login");

            entity.Property(u => u.DeletedAt)
                .HasColumnName("deleted_at");
        });
    }
}