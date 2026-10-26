using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Roles table - same names as Part 1: PK_Roles, UQ_Roles_RoleName, CK_Roles_RoleName.
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", t =>
        {
            t.HasComment("The two user roles RaceDay supports: Organiser and Participant.");
            t.HasCheckConstraint("CK_Roles_RoleName", "RoleName IN (N'Organiser', N'Participant')");
        });

        builder.HasKey(r => r.RoleId).HasName("PK_Roles");
        builder.Property(r => r.RoleId).HasComment("Primary key (identity).");
        builder.Property(r => r.RoleName).HasMaxLength(20).IsRequired()
            .HasComment("Role name: Organiser or Participant. Unique.");

        builder.HasIndex(r => r.RoleName).IsUnique().HasDatabaseName("UQ_Roles_RoleName");
    }
}
