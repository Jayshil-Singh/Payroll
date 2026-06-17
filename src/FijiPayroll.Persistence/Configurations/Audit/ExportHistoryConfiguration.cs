using FijiPayroll.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

internal sealed class ExportHistoryConfiguration : IEntityTypeConfiguration<ExportHistory>
{
    public void Configure(EntityTypeBuilder<ExportHistory> builder)
    {
        builder.ToTable("ExportHistories", "audit");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Report)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.User)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Date)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.Filter)
               .HasColumnType("nvarchar(max)")
               .IsRequired();

        builder.Property(x => x.RecordCount)
               .IsRequired();

        builder.Property(x => x.ExportType)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.DownloadCount)
               .IsRequired();

        builder.Property(x => x.IPAddress)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.GeneratedByVersion)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.Hash)
               .HasColumnType("nvarchar(64)")
               .IsRequired();
    }
}
