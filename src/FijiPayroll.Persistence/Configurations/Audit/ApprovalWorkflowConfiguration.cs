using FijiPayroll.Domain.Entities.Audit;
using FijiPayroll.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

/// <summary>
/// EF Core configuration mapping for the ApprovalWorkflow entity.
/// </summary>
internal sealed class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        builder.ToTable("ApprovalWorkflows", "audit");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.WorkflowId)
               .IsRequired();

        builder.HasIndex(x => x.WorkflowId)
               .IsUnique();

        builder.Property(x => x.EntityType)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.EntityId)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.CurrentState)
               .HasConversion<string>()
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.RequestedBy)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.CurrentApproverRole)
               .HasColumnType("nvarchar(100)")
               .IsRequired(false);

        builder.Property(x => x.CreatedDate)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.CompanyId)
               .IsRequired();

        // One-to-many navigation configuration using natural WorkflowId
        builder.HasMany(x => x.Steps)
               .WithOne()
               .HasForeignKey(x => x.WorkflowId)
               .HasPrincipalKey(x => x.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);

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
