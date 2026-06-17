using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

internal sealed class PayrollComponentDependencyConfiguration : IEntityTypeConfiguration<PayrollComponentDependency>
{
    public void Configure(EntityTypeBuilder<PayrollComponentDependency> builder)
    {
        builder.ToTable("PayrollComponentDependencies", "company");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ParentComponentId)
               .IsRequired();

        builder.Property(x => x.ChildComponentId)
               .IsRequired();

        builder.Property(x => x.DependencyType)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.CalculationOrder)
               .IsRequired();

        builder.Property(x => x.Required)
               .HasDefaultValue(true)
               .IsRequired();

        builder.HasOne(x => x.ParentComponent)
               .WithMany(x => x.Dependencies)
               .HasForeignKey(x => x.ParentComponentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ChildComponent)
               .WithMany()
               .HasForeignKey(x => x.ChildComponentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ParentComponentId, x.ChildComponentId }).IsUnique();
    }
}
