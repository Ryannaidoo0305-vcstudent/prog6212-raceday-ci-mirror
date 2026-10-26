using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// The EF Core database context. Code-first: the migration builds RaceDayApiDB from these classes.
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One configuration class per table, in Data/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RaceDayDbContext).Assembly);

        // The same seed rows as the Part 1 SQL script.
        SeedData.Apply(modelBuilder);
    }
}
