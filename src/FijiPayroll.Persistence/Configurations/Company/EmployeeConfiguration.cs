using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

/// <summary>
/// EF Core mapping configuration for the Employee entity.
/// </summary>
internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", "company");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Concurrency token
        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // Properties mapping
        builder.Property(x => x.FullName)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.Tin)
               .HasColumnType("nvarchar(20)")
               .IsRequired();

        builder.Property(x => x.FnpfNumber)
               .HasColumnType("nvarchar(20)")
               .IsRequired();

        builder.Property(x => x.ResidencyStatus)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.Department)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.BaseSalary)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.Frequency)
               .HasColumnType("nvarchar(20)")
               .IsRequired()
               .HasConversion<string>();

        builder.Property(x => x.IsFnpfExempt)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.IsTaxExempt)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // Soft Delete
        builder.Property(x => x.IsDeleted)
               .IsRequired()
               .HasDefaultValue(false);
        
        builder.HasQueryFilter(x => !x.IsDeleted);

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
    }
}
