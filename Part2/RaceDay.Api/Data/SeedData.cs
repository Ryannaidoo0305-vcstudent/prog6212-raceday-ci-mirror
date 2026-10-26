using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// The same sample data as STEP 10-12 of the Part 1 SQL script.
// Password for every demo user: RaceDay@2026 (only the BCrypt hash is stored).
public static class SeedData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" });

        modelBuilder.Entity<EventType>().HasData(
            new EventType { EventTypeId = 1, TypeName = "Run" },
            new EventType { EventTypeId = 2, TypeName = "Walk" },
            new EventType { EventTypeId = 3, TypeName = "Cycle" });

        modelBuilder.Entity<User>().HasData(
            // Organisers
            new User { UserId = 1, RoleId = 1, FirstName = "Thandi", LastName = "Mokoena", Email = "thandi.mokoena@example.com",
                PasswordHash = "$2a$11$wj6inT9j12Da9caXJQflmeMsmucCODikYIMnxXSBMGERv1jumu7Cu", PhoneNumber = "082 555 0101",
                CreatedAt = new DateTime(2026, 5, 4, 8, 30, 0) },
            new User { UserId = 2, RoleId = 1, FirstName = "Pieter", LastName = "van der Merwe", Email = "pieter.vdmerwe@example.com",
                PasswordHash = "$2a$11$NNCYCv1wLHPIUvg8MGnTN.HUU2OZOwV2Qd13VYpdil3oLF/rLv3fO", PhoneNumber = "083 555 0102",
                CreatedAt = new DateTime(2026, 5, 6, 10, 15, 0) },
            // Participants
            new User { UserId = 3, RoleId = 2, FirstName = "Sipho", LastName = "Ndlovu", Email = "sipho.ndlovu@example.com",
                PasswordHash = "$2a$11$dm1FMgHlLr0oA0VHvEKvPOU01M8Khxk.DgH38ey0QEcIV5DW6ZPim", PhoneNumber = "071 555 0103",
                DateOfBirth = new DateOnly(1994, 3, 12), CreatedAt = new DateTime(2026, 5, 20, 19, 5, 0) },
            new User { UserId = 4, RoleId = 2, FirstName = "Aisha", LastName = "Patel", Email = "aisha.patel@example.com",
                PasswordHash = "$2a$11$O1cENnzJQBo2uhqU.hST.uYtwy9wyjkkL7XSgu6owWjZoZtdwbNNe", PhoneNumber = "072 555 0104",
                DateOfBirth = new DateOnly(2001, 7, 25), CreatedAt = new DateTime(2026, 6, 18, 12, 40, 0) },
            new User { UserId = 5, RoleId = 2, FirstName = "Lerato", LastName = "Dlamini", Email = "lerato.dlamini@example.com",
                PasswordHash = "$2a$11$WBIcsPrlpT75IFs/QnTcPezlpOEfRgLdZfWw8g2iYkB2TIBBDL40K", PhoneNumber = "073 555 0105",
                DateOfBirth = new DateOnly(1983, 11, 2), CreatedAt = new DateTime(2026, 6, 9, 7, 15, 0) },
            new User { UserId = 6, RoleId = 2, FirstName = "Johan", LastName = "Botha", Email = "johan.botha@example.com",
                PasswordHash = "$2a$11$cS2gwsbPMHklOit.zMrDUeLVThMB7M6MzHjxiEkwG90NYGQ8ftfPe", PhoneNumber = "074 555 0106",
                DateOfBirth = new DateOnly(1971, 5, 30), CreatedAt = new DateTime(2026, 6, 28, 17, 50, 0) },
            new User { UserId = 7, RoleId = 2, FirstName = "Kabelo", LastName = "Molefe", Email = "kabelo.molefe@example.com",
                PasswordHash = "$2a$11$VGLhem1qcFmVdgwc2ckbm.vyeF.j6Qeys3gKb6aNtcos5KXiTBc2O", PhoneNumber = "076 555 0107",
                DateOfBirth = new DateOnly(2007, 10, 15), CreatedAt = new DateTime(2026, 6, 3, 15, 25, 0) });

        modelBuilder.Entity<Event>().HasData(
            new Event { EventId = 1, OrganiserId = 1, EventTypeId = 1, Name = "Jozi Sunrise Half Marathon",
                Description = "A fast 21.1km road race through the tree-lined northern suburbs, starting and finishing at Zoo Lake. Water tables every 3km and a 3h30 cut-off.",
                EventDate = new DateTime(2026, 8, 23, 6, 0, 0), Location = "Zoo Lake, Parkview, Johannesburg", DistanceKm = 21.10m,
                CreatedAt = new DateTime(2026, 5, 10, 9, 0, 0) },
            new Event { EventId = 2, OrganiserId = 2, EventTypeId = 3, Name = "Hartbeespoort Cycle Classic",
                Description = "A scenic road cycling classic around the Hartbeespoort Dam with 100km, 60km and 30km route options, rolling road closures and a finish-line festival.",
                EventDate = new DateTime(2027, 3, 14, 6, 30, 0), Location = "Hartbeespoort Dam, North West", DistanceKm = 100.00m,
                CreatedAt = new DateTime(2026, 6, 1, 12, 0, 0) },
            new Event { EventId = 3, OrganiserId = 2, EventTypeId = 2, Name = "Durban Beachfront Charity Walk",
                Description = "A relaxed family walk along the Golden Mile promenade raising funds for local children's charities. Dogs on leads welcome.",
                EventDate = new DateTime(2027, 2, 7, 7, 0, 0), Location = "uShaka Beach, Durban", DistanceKm = 8.00m,
                CreatedAt = new DateTime(2026, 6, 15, 14, 30, 0) },
            new Event { EventId = 4, OrganiserId = 1, EventTypeId = 1, Name = "Soweto Heritage 10K",
                Description = "A 10km road race past Vilakazi Street and other Soweto landmarks, finishing inside Orlando Stadium.",
                EventDate = new DateTime(2027, 4, 25, 6, 30, 0), Location = "Orlando Stadium, Soweto", DistanceKm = 10.00m,
                CreatedAt = new DateTime(2026, 7, 1, 8, 0, 0) });

        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryId = 1, EventId = 1, Name = "Junior (Under 20)", CategoryType = "Age", MinAge = 16, MaxAge = 19, MaxParticipants = 300 },
            new Category { CategoryId = 2, EventId = 1, Name = "Senior (20-39)", CategoryType = "Age", MinAge = 20, MaxAge = 39, MaxParticipants = 1500 },
            new Category { CategoryId = 3, EventId = 1, Name = "Veteran (40-49)", CategoryType = "Age", MinAge = 40, MaxAge = 49, MaxParticipants = 800 },
            new Category { CategoryId = 4, EventId = 1, Name = "Master (50+)", CategoryType = "Age", MinAge = 50, MaxParticipants = 800 },
            new Category { CategoryId = 5, EventId = 2, Name = "100km Classic", CategoryType = "Distance", DistanceKm = 100.00m, MaxParticipants = 1000 },
            new Category { CategoryId = 6, EventId = 2, Name = "60km Challenge", CategoryType = "Distance", DistanceKm = 60.00m, MaxParticipants = 800 },
            new Category { CategoryId = 7, EventId = 2, Name = "30km Fun Ride", CategoryType = "Distance", DistanceKm = 30.00m },
            new Category { CategoryId = 8, EventId = 3, Name = "8km Charity Walk", CategoryType = "Distance", DistanceKm = 8.00m },
            new Category { CategoryId = 9, EventId = 3, Name = "4km Family Walk", CategoryType = "Distance", DistanceKm = 4.00m },
            new Category { CategoryId = 10, EventId = 4, Name = "Junior (Under 20)", CategoryType = "Age", MinAge = 13, MaxAge = 19, MaxParticipants = 400 },
            new Category { CategoryId = 11, EventId = 4, Name = "Senior (20-39)", CategoryType = "Age", MinAge = 20, MaxAge = 39, MaxParticipants = 2000 },
            new Category { CategoryId = 12, EventId = 4, Name = "Veteran (40-49)", CategoryType = "Age", MinAge = 40, MaxAge = 49, MaxParticipants = 1000 },
            new Category { CategoryId = 13, EventId = 4, Name = "Master (50+)", CategoryType = "Age", MinAge = 50, MaxParticipants = 1000 });

        modelBuilder.Entity<Enrolment>().HasData(
            // Event 1 (finished): everyone confirmed and finished
            new Enrolment { EnrolmentId = 1, ParticipantId = 3, EventId = 1, CategoryId = 2, Status = "Confirmed", EnrolledAt = new DateTime(2026, 6, 2, 19, 45, 0) },
            new Enrolment { EnrolmentId = 2, ParticipantId = 7, EventId = 1, CategoryId = 1, Status = "Confirmed", EnrolledAt = new DateTime(2026, 6, 5, 12, 10, 0) },
            new Enrolment { EnrolmentId = 3, ParticipantId = 5, EventId = 1, CategoryId = 3, Status = "Confirmed", EnrolledAt = new DateTime(2026, 6, 11, 7, 30, 0) },
            new Enrolment { EnrolmentId = 4, ParticipantId = 4, EventId = 1, CategoryId = 2, Status = "Confirmed", EnrolledAt = new DateTime(2026, 6, 20, 21, 5, 0) },
            new Enrolment { EnrolmentId = 5, ParticipantId = 6, EventId = 1, CategoryId = 4, Status = "Confirmed", EnrolledAt = new DateTime(2026, 7, 1, 6, 50, 0) },
            // Upcoming events: a mix of statuses
            new Enrolment { EnrolmentId = 6, ParticipantId = 6, EventId = 2, CategoryId = 5, Status = "Confirmed", EnrolledAt = new DateTime(2026, 8, 30, 10, 0, 0) },
            new Enrolment { EnrolmentId = 7, ParticipantId = 4, EventId = 2, CategoryId = 7, Status = "Pending", EnrolledAt = new DateTime(2026, 9, 12, 16, 20, 0) },
            new Enrolment { EnrolmentId = 8, ParticipantId = 5, EventId = 3, CategoryId = 8, Status = "Confirmed", EnrolledAt = new DateTime(2026, 9, 3, 18, 0, 0) },
            new Enrolment { EnrolmentId = 9, ParticipantId = 3, EventId = 4, CategoryId = 11, Status = "Pending", EnrolledAt = new DateTime(2026, 9, 20, 20, 15, 0) },
            new Enrolment { EnrolmentId = 10, ParticipantId = 7, EventId = 4, CategoryId = 10, Status = "Cancelled", EnrolledAt = new DateTime(2026, 9, 8, 13, 40, 0) });

        var recorded = new DateTime(2026, 8, 23, 13, 30, 0);
        modelBuilder.Entity<Result>().HasData(
            new Result { ResultId = 1, EnrolmentId = 1, FinishTime = new TimeSpan(1, 21, 37), FinishPosition = 1, IsPublished = true, RecordedAt = recorded },
            new Result { ResultId = 2, EnrolmentId = 2, FinishTime = new TimeSpan(1, 29, 5), FinishPosition = 2, IsPublished = true, RecordedAt = recorded },
            new Result { ResultId = 3, EnrolmentId = 3, FinishTime = new TimeSpan(1, 47, 52), FinishPosition = 3, IsPublished = true, RecordedAt = recorded },
            new Result { ResultId = 4, EnrolmentId = 4, FinishTime = new TimeSpan(1, 58, 14), FinishPosition = 4, IsPublished = true, RecordedAt = recorded },
            new Result { ResultId = 5, EnrolmentId = 5, FinishTime = new TimeSpan(2, 11, 40), FinishPosition = 5, IsPublished = true, RecordedAt = recorded });
    }
}
