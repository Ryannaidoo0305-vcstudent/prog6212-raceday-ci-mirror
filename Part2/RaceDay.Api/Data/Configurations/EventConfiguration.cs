using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Events table - each event belongs to one Organiser.
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", t =>
        {
            t.HasComment("A run, walk or cycle event, created and owned by one Organiser.");
            t.HasCheckConstraint("CK_Events_DistanceKm", "DistanceKm > 0");
        });

        builder.HasKey(e => e.EventId).HasName("PK_Events");
        builder.Property(e => e.EventId).HasComment("Primary key (identity).");
        builder.Property(e => e.OrganiserId).HasComment("The Organiser who owns the event (FK to Users).");
        builder.Property(e => e.EventTypeId).HasComment("Run, Walk or Cycle (FK to EventTypes).");
        builder.Property(e => e.Name).HasMaxLength(150).IsRequired().HasComment("Event name shown to participants.");
        builder.Property(e => e.Description).HasMaxLength(2000).IsRequired().HasComment("Full description of the event.");
        builder.Property(e => e.EventDate).HasColumnType("datetime2").HasComment("Race date and start time.");
        builder.Property(e => e.Location).HasMaxLength(200).IsRequired().HasComment("Venue or start location.");
        builder.Property(e => e.DistanceKm).HasPrecision(6, 2).HasComment("Main (longest) distance in km. Must be above 0.");
        builder.Property(e => e.BannerImageUrl).HasMaxLength(500)
            .HasComment("Azure Blob Storage URL of the event banner (added in Part 3).");
        builder.Property(e => e.CreatedAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_Events_CreatedAt")
            .HasComment("When the event was created (UTC).");

        builder.HasIndex(e => e.EventDate).HasDatabaseName("IX_Events_EventDate");

        builder.HasOne(e => e.Organiser).WithMany(u => u.OrganisedEvents)
            .HasForeignKey(e => e.OrganiserId)
            .HasConstraintName("FK_Events_Organiser")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.EventType).WithMany(t => t.Events)
            .HasForeignKey(e => e.EventTypeId)
            .HasConstraintName("FK_Events_EventTypes")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
