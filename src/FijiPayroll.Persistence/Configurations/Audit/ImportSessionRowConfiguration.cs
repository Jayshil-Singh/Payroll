using FijiPayroll.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

internal sealed class ImportSessionRowConfiguration : IEntityTypeConfiguration<ImportSessionRow>
{
    public void Configure(EntityTypeBuilder<ImportSessionRow> builder)
    {
        builder.ToTable("ImportSessionRows", "audit");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ImportSessionId)
               .IsRequired();

        builder.Property(x => x.RowNumber)
               .IsRequired();

        builder.Property(x => x.Payload)
               .HasColumnType("nvarchar(max)")
               .IsRequired();

        builder.Property(x => x.ValidationStatus)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.Errors)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        builder.Property(x => x.Warnings)
               .HasColumnType("nvarchar(max)")
               .IsRequired(false);

        builder.HasOne(x => x.ImportSession)
               .WithMany(x => x.Rows)
               .HasForeignKey(x => x.ImportSessionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ImportSessionId, x.RowNumber }).IsUnique();
    }
}
