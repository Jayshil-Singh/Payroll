using FijiPayroll.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

/// <summary>
/// EF Core configuration mapping for the SearchIndex entity.
/// </summary>
internal sealed class SearchIndexConfiguration : IEntityTypeConfiguration<SearchIndex>
{
    public void Configure(EntityTypeBuilder<SearchIndex> builder)
    {
        builder.ToTable("SearchIndexes", "audit");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.SearchId)
               .IsRequired();

        builder.HasIndex(x => x.SearchId)
               .IsUnique();

        builder.Property(x => x.EntityType)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.EntityId)
               .HasColumnType("nvarchar(100)")
               .IsRequired();

        builder.Property(x => x.Content)
               .HasColumnType("nvarchar(max)")
               .IsRequired();

        builder.Property(x => x.WeightedScore)
               .IsRequired();

        builder.Property(x => x.LastUpdated)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.CompanyId)
               .IsRequired();

        // Unique index for multi-tenant entity lookup speed and constraint
        builder.HasIndex(x => new { x.CompanyId, x.EntityType, x.EntityId })
               .IsUnique();
    }
}
