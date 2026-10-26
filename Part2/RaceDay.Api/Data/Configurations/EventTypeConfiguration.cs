using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// EventTypes table - PK_EventTypes, UQ_EventTypes_TypeName.
public class EventTypeConfiguration : IEntityTypeConfiguration<EventType>
{
    public void Configure(EntityTypeBuilder<EventType> builder)
    {
        builder.ToTable("EventTypes", t => t.HasComment("Kinds of event: Run, Walk or Cycle."));

        builder.HasKey(e => e.EventTypeId).HasName("PK_EventTypes");
        builder.Property(e => e.EventTypeId).HasComment("Primary key (identity).");
        builder.Property(e => e.TypeName).HasMaxLength(20).IsRequired()
            .HasComment("Event type name: Run, Walk or Cycle. Unique.");

        builder.HasIndex(e => e.TypeName).IsUnique().HasDatabaseName("UQ_EventTypes_TypeName");
    }
}
