using FijiPayroll.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

/// <summary>
/// EF Core configuration mapping for the WorkflowStepLog entity.
/// </summary>
internal sealed class WorkflowStepLogConfiguration : IEntityTypeConfiguration<WorkflowStepLog>
{
    public void Configure(EntityTypeBuilder<WorkflowStepLog> builder)
    {
        builder.ToTable("WorkflowStepLogs", "audit");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.LogId)
               .IsRequired();

        builder.HasIndex(x => x.LogId)
               .IsUnique();

        builder.Property(x => x.WorkflowId)
               .IsRequired();

        builder.Property(x => x.FromState)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.ToState)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.TransitionedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.TransitionedAt)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.Comments)
               .HasColumnType("nvarchar(max)")
               .IsRequired();
    }
}
