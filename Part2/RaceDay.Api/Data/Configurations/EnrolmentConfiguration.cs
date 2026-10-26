using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Enrolments table - the bridge table between Participants and Events.
// UQ_Enrolments_Participant_Event stops anyone entering the same event twice.
public class EnrolmentConfiguration : IEntityTypeConfiguration<Enrolment>
{
    public void Configure(EntityTypeBuilder<Enrolment> builder)
    {
        builder.ToTable("Enrolments", t =>
        {
            t.HasComment("An entry by a Participant into an event in a chosen category. Resolves the many-to-many between Users and Events.");
            t.HasCheckConstraint("CK_Enrolments_Status", "Status IN (N'Pending', N'Confirmed', N'Cancelled')");
        });

        builder.HasKey(e => e.EnrolmentId).HasName("PK_Enrolments");
        builder.Property(e => e.EnrolmentId).HasComment("Primary key (identity).");
        builder.Property(e => e.ParticipantId).HasComment("The Participant who entered (FK to Users).");
        builder.Property(e => e.EventId).HasComment("The event entered (FK to Events). One entry per participant per event.");
        builder.Property(e => e.CategoryId).HasComment("The category chosen (FK to Categories).");
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired()
            .HasDefaultValue("Pending", "DF_Enrolments_Status")
            .HasComment("Pending, Confirmed or Cancelled. Starts as Pending.");
        builder.Property(e => e.EnrolledAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_Enrolments_EnrolledAt")
            .HasComment("When the entry was made (UTC).");

        builder.HasIndex(e => new { e.ParticipantId, e.EventId }).IsUnique()
            .HasDatabaseName("UQ_Enrolments_Participant_Event");

        builder.HasOne(e => e.Participant).WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.ParticipantId)
            .HasConstraintName("FK_Enrolments_Participant")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Event).WithMany(ev => ev.Enrolments)
            .HasForeignKey(e => e.EventId)
            .HasConstraintName("FK_Enrolments_Events")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Category).WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId)
            .HasConstraintName("FK_Enrolments_Categories")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
