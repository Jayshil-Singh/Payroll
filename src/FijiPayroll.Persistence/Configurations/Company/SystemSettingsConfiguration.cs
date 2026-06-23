using FijiPayroll.Domain.Entities.Company;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Company;

internal sealed class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("SystemSettings", "company");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.DefaultPayFrequency)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.DefaultPayrollCalendar)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.NegativePayPolicy)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(x => x.DefaultSubmissionPaths)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.Property(x => x.BackupDirectory)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.Property(x => x.ExportDirectory)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.Property(x => x.ImportDirectory)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.Property(x => x.SmtpHost)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.SmtpPort)
               .IsRequired();

        builder.Property(x => x.SmtpUsername)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.SmtpPassword)
               .HasColumnType("nvarchar(500)")
               .IsRequired();

        builder.Property(x => x.SmtpSslEnabled)
               .IsRequired();

        builder.HasIndex(x => x.CompanyId);
    }
}
