using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

internal sealed class RuleModuleConfiguration : IEntityTypeConfiguration<RuleModule>
{
    public void Configure(EntityTypeBuilder<RuleModule> builder)
    {
        builder.ToTable("RuleModules", "company");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Code)
               .HasColumnType("nvarchar(50)")
               .IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Name)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Description)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        builder.Property(x => x.ExecutionPriority)
               .IsRequired();

        builder.Property(x => x.IsSystem)
               .HasDefaultValue(false)
               .IsRequired();
    }
}
