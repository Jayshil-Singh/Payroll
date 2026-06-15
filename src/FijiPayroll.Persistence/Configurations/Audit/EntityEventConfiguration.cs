using FijiPayroll.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FijiPayroll.Persistence.Configurations.Audit;

/// <summary>
/// EF Core configuration mapping for the EntityEvent outbox entity.
/// </summary>
internal sealed class EntityEventConfiguration : IEntityTypeConfiguration<EntityEvent>
{
    public void Configure(EntityTypeBuilder<EntityEvent> builder)
    {
        builder.ToTable("EntityEvents", "audit");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.CompanyId)
               .IsRequired();

        builder.Property(x => x.EventType)
               .HasColumnType("nvarchar(200)")
               .IsRequired();

        builder.Property(x => x.Payload)
               .HasColumnType("nvarchar(max)")
               .IsRequired();

        builder.Property(x => x.OccurredOn)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(x => x.CorrelationId)
               .IsRequired();
    }
}
