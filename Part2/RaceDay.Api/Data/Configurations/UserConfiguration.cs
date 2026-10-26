using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Users table - one table for both roles, told apart by RoleId.
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t =>
        {
            t.HasComment("Everyone who can log in. Organisers and Participants share this table and are told apart by RoleId.");
            t.HasCheckConstraint("CK_Users_Email", "Email LIKE N'%_@_%._%'");
            t.HasCheckConstraint("CK_Users_DateOfBirth",
                "DateOfBirth IS NULL OR (DateOfBirth >= '1900-01-01' AND DateOfBirth < CAST(SYSUTCDATETIME() AS DATE))");
        });

        builder.HasKey(u => u.UserId).HasName("PK_Users");
        builder.Property(u => u.UserId).HasComment("Primary key (identity).");
        builder.Property(u => u.RoleId).HasComment("Role of the user (FK to Roles). Chosen once at registration.");
        builder.Property(u => u.FirstName).HasMaxLength(50).IsRequired().HasComment("First name.");
        builder.Property(u => u.LastName).HasMaxLength(50).IsRequired().HasComment("Surname.");
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired()
            .HasComment("Login name. Unique, ignoring letter case, and must look like an email address.");
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired()
            .HasComment("BCrypt hash of the password. The plain-text password is never stored.");
        builder.Property(u => u.PhoneNumber).HasMaxLength(20).HasComment("Contact number (optional).");
        builder.Property(u => u.DateOfBirth).HasColumnType("date")
            .HasComment("Date of birth, used to check age categories. Must be in the past.");
        builder.Property(u => u.ProfilePictureUrl).HasMaxLength(500)
            .HasComment("Azure Blob Storage URL of the profile picture (added in Part 3).");
        builder.Property(u => u.CreatedAt).HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()", "DF_Users_CreatedAt")
            .HasComment("When the account was created (UTC).");

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");

        builder.HasOne(u => u.Role).WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .HasConstraintName("FK_Users_Roles")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
