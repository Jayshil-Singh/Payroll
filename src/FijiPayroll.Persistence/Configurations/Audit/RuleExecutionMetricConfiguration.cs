using FijiPayroll.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

internal sealed class RuleExecutionMetricConfiguration : IEntityTypeConfiguration<RuleExecutionMetric>
{
    public void Configure(EntityTypeBuilder<RuleExecutionMetric> builder)
    {
        builder.ToTable("RuleExecutionMetrics", "audit");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.RuleId)
               .IsRequired();

        builder.Property(x => x.ExecutionCount)
               .IsRequired();

        builder.Property(x => x.AverageExecutionTime)
               .IsRequired();

        builder.Property(x => x.MaximumExecutionTime)
               .IsRequired();

        builder.Property(x => x.FailureCount)
               .IsRequired();

        builder.Property(x => x.LastExecuted)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.HasIndex(x => x.RuleId).IsUnique();
    }
}
