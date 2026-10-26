using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Results table - one result per enrolment (1 to 0..1), deleted with its enrolment.
public class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.ToTable("Results", t =>
        {
            t.HasComment("Finish time and position for one enrolment, captured by the Organiser after the event.");
            t.HasCheckConstraint("CK_Results_FinishTime", "FinishTime > '00:00:00'");
            t.HasCheckConstraint("CK_Results_FinishPosition", "FinishPosition > 0");
        });

        builder.HasKey(r => r.ResultId).HasName("PK_Results");
        builder.Property(r => r.ResultId).HasComment("Primary key (identity).");
        builder.Property(r => r.EnrolmentId).HasComment("The entry this result belongs to (FK to Enrolments). One result per entry.");
        builder.Property(r => r.FinishTime).HasColumnType("time(0)").HasComment("Finish time as hh:mm:ss. Must be above zero.");
        builder.Property(r => r.FinishPosition).HasComment("Overall finishing position. Must be 1 or more.");
        builder.Property(r => r.IsPublished)
            .HasDefaultValue(false, "DF_Results_IsPublished")
            .HasComment("1 once the Organiser publishes the results; participants only see published results.");
        builder.Property(r => r.RecordedAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_Results_RecordedAt")
            .HasComment("When the result was captured (UTC).");

        builder.HasIndex(r => r.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_EnrolmentId");

        builder.HasOne(r => r.Enrolment).WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .HasConstraintName("FK_Results_Enrolments")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
