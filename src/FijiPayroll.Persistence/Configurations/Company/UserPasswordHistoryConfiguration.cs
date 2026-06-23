using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for UserPasswordHistory entity.
/// </summary>
internal sealed class UserPasswordHistoryConfiguration : IEntityTypeConfiguration<UserPasswordHistory>
{
    public void Configure(EntityTypeBuilder<UserPasswordHistory> builder)
    {
        builder.ToTable("UserPasswordHistories", "company");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Columns
        builder.Property(x => x.UserAccountId)
               .IsRequired();

        builder.Property(x => x.PasswordHash)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.CreatedAt)
               .HasColumnType("datetime2")
               .IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserAccountId);
    }
}
