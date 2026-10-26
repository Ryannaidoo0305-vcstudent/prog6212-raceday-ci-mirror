using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Categories table - Age or Distance categories, deleted together with their event.
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t =>
        {
            t.HasComment("An age or distance category that Participants choose when they enter an event.");
            t.HasCheckConstraint("CK_Categories_CategoryType", "CategoryType IN (N'Age', N'Distance')");
            t.HasCheckConstraint("CK_Categories_AgeRange", "MinAge IS NULL OR MaxAge IS NULL OR MinAge <= MaxAge");
            t.HasCheckConstraint("CK_Categories_AgeNeedsLimit", "CategoryType <> N'Age' OR MinAge IS NOT NULL OR MaxAge IS NOT NULL");
            t.HasCheckConstraint("CK_Categories_DistanceNeedsKm", "CategoryType <> N'Distance' OR DistanceKm IS NOT NULL");
            t.HasCheckConstraint("CK_Categories_DistanceKm", "DistanceKm IS NULL OR DistanceKm > 0");
            t.HasCheckConstraint("CK_Categories_MaxParticipants", "MaxParticipants IS NULL OR MaxParticipants > 0");
        });

        builder.HasKey(c => c.CategoryId).HasName("PK_Categories");
        builder.Property(c => c.CategoryId).HasComment("Primary key (identity).");
        builder.Property(c => c.EventId).HasComment("The event this category belongs to (FK to Events). Deleted with its event.");
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired().HasComment("Category name, unique within its event.");
        builder.Property(c => c.CategoryType).HasMaxLength(10).IsRequired().HasComment("Age or Distance.");
        builder.Property(c => c.MinAge).HasComment("Youngest age allowed on race day (Age categories).");
        builder.Property(c => c.MaxAge).HasComment("Oldest age allowed on race day; empty means no upper limit (Age categories).");
        builder.Property(c => c.DistanceKm).HasPrecision(6, 2).HasComment("Distance in km (Distance categories). Must be above 0.");
        builder.Property(c => c.MaxParticipants).HasComment("Entry limit; empty means unlimited.");

        builder.HasIndex(c => new { c.EventId, c.Name }).IsUnique().HasDatabaseName("UQ_Categories_Event_Name");

        builder.HasOne(c => c.Event).WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .HasConstraintName("FK_Categories_Events")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
