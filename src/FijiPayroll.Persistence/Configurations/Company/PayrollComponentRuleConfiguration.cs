using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

internal sealed class PayrollComponentRuleConfiguration : IEntityTypeConfiguration<PayrollComponentRule>
{
    public void Configure(EntityTypeBuilder<PayrollComponentRule> builder)
    {
        builder.ToTable("PayrollComponentRules", "company");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ComponentId)
               .IsRequired();

        builder.Property(x => x.RuleModuleId)
               .IsRequired();

        builder.Property(x => x.RuleType)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.ExpressionText)
               .HasColumnType("nvarchar(max)")
               .IsRequired();

        builder.Property(x => x.CompiledHash)
               .HasColumnType("nvarchar(64)")
               .IsRequired();

        builder.Property(x => x.CompiledVersion)
               .IsRequired();

        builder.Property(x => x.Priority)
               .IsRequired();

        builder.Property(x => x.EffectiveFrom)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.EffectiveTo)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.RuleVersion)
               .IsRequired();

        // Navigations
        builder.HasOne(x => x.Component)
               .WithMany(x => x.Rules)
               .HasForeignKey(x => x.ComponentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RuleModule)
               .WithMany()
               .HasForeignKey(x => x.RuleModuleId)
               .OnDelete(DeleteBehavior.Restrict);

        // Auditable fields
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

        builder.Property(x => x.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        builder.HasIndex(x => new { x.ComponentId, x.EffectiveFrom });
    }
}
