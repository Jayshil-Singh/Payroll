using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core configuration mapping for UserPermission entity.
/// </summary>
internal sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions", "company");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Columns
        builder.Property(x => x.UserRoleId)
               .IsRequired();

        builder.Property(x => x.PermissionCode)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserRoleId);
    }
}
