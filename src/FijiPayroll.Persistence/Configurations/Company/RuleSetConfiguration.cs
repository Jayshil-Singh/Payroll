using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

internal sealed class RuleSetConfiguration : IEntityTypeConfiguration<RuleSet>
{
    public void Configure(EntityTypeBuilder<RuleSet> builder)
    {
        builder.ToTable("RuleSets", "company");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Description)
               .HasColumnType("nvarchar(500)")
               .IsRequired(false);

        builder.Property(x => x.Version)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.EffectiveFrom)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.EffectiveTo)
               .HasColumnType("datetime2")
               .IsRequired(false);

        builder.Property(x => x.Status)
               .HasColumnType("nvarchar(20)")
               .HasConversion<string>()
               .IsRequired();

        builder.Property(x => x.CompanyId)
               .IsRequired();

        builder.Property(x => x.ParentRuleSetId)
               .IsRequired(false);

        builder.Property(x => x.IsSystem)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(x => x.IsLocked)
               .HasDefaultValue(false)
               .IsRequired();

        builder.HasOne(x => x.ParentRuleSet)
               .WithMany(x => x.ChildRuleSets)
               .HasForeignKey(x => x.ParentRuleSetId)
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
    }
}
