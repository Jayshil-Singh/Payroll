using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for UserRole entity.
/// </summary>
internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "company");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Columns
        builder.Property(x => x.UserAccountId)
               .IsRequired();

        builder.Property(x => x.RoleName)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserAccountId);

        // Configure relationships
        builder.HasMany(x => x.Permissions)
               .WithOne()
               .HasForeignKey(x => x.UserRoleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
