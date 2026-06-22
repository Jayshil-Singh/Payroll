using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for UserAccount entity.
/// </summary>
internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts", "company");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Concurrency token
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // Columns
        builder.Property(x => x.CompanyId)
               .IsRequired();

        builder.Property(x => x.Username)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.PasswordHash)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.DisplayName)
               .HasColumnType("nvarchar(150)")
               .IsRequired();

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(x => x.IsSystemAdmin)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.LastLoginAt)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.FailedLoginCount)
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(x => x.LockedUntil)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.MustChangePassword)
               .IsRequired()
               .HasDefaultValue(false);

        // Audit Columns
        builder.Property(x => x.CreatedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.CreatedAt)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.ModifiedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.ModifiedAt)
               .HasColumnType("datetime2")
               .IsRequired(false);

        // Index unique username per company
        builder.HasIndex(x => new { x.CompanyId, x.Username })
               .IsUnique();

        // Configure relationships
        builder.HasMany(x => x.Roles)
               .WithOne()
               .HasForeignKey(x => x.UserAccountId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
