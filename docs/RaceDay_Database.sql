/* =====================================================================
   RaceDay - Event Management System for SA road running, walking and cycling
   PROG6212 Programming 2B - POE Part 1, Section C: SQL Database Script
   Student : Ryan Naidoo (ST10487315)
   Target  : Microsoft SQL Server 2019 or later - open in SSMS and press F5

   What this script does
     STEP 1        Drops and recreates the RaceDayDB database (safe to re-run)
     STEP 2 - 7    Creates the 7 tables shown in docs/RaceDay_ERD.png
     STEP 8        Adds indexes on foreign key columns
     STEP 9        Stores a description of every table and column in SQL Server
     STEP 10 - 12  Seeds realistic South African sample data
     STEP 13       Runs verification queries so you can see it worked

   Companion files
     docs/RaceDay_Constraint_Tests.sql  proves every constraint below works
     docs/Data_Dictionary.md            documents every column and constraint

   Constraint names: PK_ primary key   FK_ foreign key   UQ_ unique
                     CK_ check         DF_ default       IX_ index

   Demo password for every seeded user: RaceDay@2026
   Only its BCrypt hash is stored - never the plain-text password.
   ===================================================================== */


-- =====================================================================
-- STEP 1: Create a clean RaceDayDB database
-- =====================================================================
USE master;
GO

IF DB_ID(N'RaceDayDB') IS NOT NULL
BEGIN
    -- Close any open connections (e.g. another SSMS tab) so the drop works
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

PRINT 'STEP 1 complete: RaceDayDB created';
GO


-- =====================================================================
-- STEP 2: Lookup tables - Roles and EventTypes
-- =====================================================================

-- Roles: the only two roles RaceDay supports.
-- The CHECK constraint means no other role (e.g. "Admin") can ever be added.
CREATE TABLE dbo.Roles
(
    RoleId          INT             IDENTITY(1,1)  NOT NULL,
    RoleName        NVARCHAR(20)    NOT NULL,

    CONSTRAINT PK_Roles             PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName    UNIQUE (RoleName),
    CONSTRAINT CK_Roles_RoleName    CHECK (RoleName IN (N'Organiser', N'Participant'))
);
GO

-- EventTypes: Run, Walk or Cycle. A table (not hard-coded text) so a new
-- type such as "Trail Run" can be added later without changing the schema.
CREATE TABLE dbo.EventTypes
(
    EventTypeId     INT             IDENTITY(1,1)  NOT NULL,
    TypeName        NVARCHAR(20)    NOT NULL,

    CONSTRAINT PK_EventTypes            PRIMARY KEY (EventTypeId),
    CONSTRAINT UQ_EventTypes_TypeName   UNIQUE (TypeName)
);
GO

PRINT 'STEP 2 complete: Roles and EventTypes created';
GO


-- =====================================================================
-- STEP 3: Users - one table for both Organisers and Participants
-- =====================================================================
CREATE TABLE dbo.Users
(
    UserId              INT             IDENTITY(1,1)  NOT NULL,
    RoleId              INT             NOT NULL,
    FirstName           NVARCHAR(50)    NOT NULL,
    LastName            NVARCHAR(50)    NOT NULL,
    Email               NVARCHAR(256)   NOT NULL,   -- used to log in
    PasswordHash        NVARCHAR(255)   NOT NULL,   -- BCrypt hash, never the real password
    PhoneNumber         NVARCHAR(20)    NULL,
    DateOfBirth         DATE            NULL,       -- places Participants in age categories
    ProfilePictureUrl   NVARCHAR(500)   NULL,       -- Azure Blob Storage URL (Part 3)
    CreatedAt           DATETIME2       NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Users             PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email       UNIQUE (Email),
    CONSTRAINT CK_Users_Email       CHECK (Email LIKE N'%_@_%._%'),
    CONSTRAINT CK_Users_DateOfBirth CHECK (DateOfBirth IS NULL OR (DateOfBirth >= '1900-01-01' AND DateOfBirth < CAST(SYSUTCDATETIME() AS DATE))),
    CONSTRAINT FK_Users_Roles       FOREIGN KEY (RoleId)
        REFERENCES dbo.Roles (RoleId) ON DELETE NO ACTION
);
GO

PRINT 'STEP 3 complete: Users created';
GO


-- =====================================================================
-- STEP 4: Events - each event is created and owned by one Organiser
-- =====================================================================
CREATE TABLE dbo.Events
(
    EventId             INT             IDENTITY(1,1)  NOT NULL,
    OrganiserId         INT             NOT NULL,   -- the Organiser (Users.UserId) who owns the event
    EventTypeId         INT             NOT NULL,
    Name                NVARCHAR(150)   NOT NULL,
    Description         NVARCHAR(2000)  NOT NULL,
    EventDate           DATETIME2       NOT NULL,   -- race date and start time
    Location            NVARCHAR(200)   NOT NULL,
    DistanceKm          DECIMAL(6,2)    NOT NULL,   -- main (longest) distance, e.g. 21.10
    BannerImageUrl      NVARCHAR(500)   NULL,       -- Azure Blob Storage URL (Part 3)
    CreatedAt           DATETIME2       NOT NULL
        CONSTRAINT DF_Events_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Events            PRIMARY KEY (EventId),
    CONSTRAINT CK_Events_DistanceKm CHECK (DistanceKm > 0),
    CONSTRAINT FK_Events_Organiser  FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT FK_Events_EventTypes FOREIGN KEY (EventTypeId)
        REFERENCES dbo.EventTypes (EventTypeId) ON DELETE NO ACTION
);
GO

PRINT 'STEP 4 complete: Events created';
GO


-- =====================================================================
-- STEP 5: Categories - age or distance categories for each event
-- =====================================================================
CREATE TABLE dbo.Categories
(
    CategoryId          INT             IDENTITY(1,1)  NOT NULL,
    EventId             INT             NOT NULL,
    Name                NVARCHAR(100)   NOT NULL,   -- e.g. 'Senior (20-39)' or '30km Fun Ride'
    CategoryType        NVARCHAR(10)    NOT NULL,   -- 'Age' or 'Distance'
    MinAge              INT             NULL,       -- Age categories only
    MaxAge              INT             NULL,       -- Age categories only (NULL = no upper limit)
    DistanceKm          DECIMAL(6,2)    NULL,       -- Distance categories only
    MaxParticipants     INT             NULL,       -- entry limit (NULL = unlimited)

    CONSTRAINT PK_Categories                    PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_Event_Name         UNIQUE (EventId, Name),
    CONSTRAINT CK_Categories_CategoryType       CHECK (CategoryType IN (N'Age', N'Distance')),
    CONSTRAINT CK_Categories_AgeRange           CHECK (MinAge IS NULL OR MaxAge IS NULL OR MinAge <= MaxAge),
    CONSTRAINT CK_Categories_AgeNeedsLimit      CHECK (CategoryType <> N'Age' OR MinAge IS NOT NULL OR MaxAge IS NOT NULL),
    CONSTRAINT CK_Categories_DistanceNeedsKm    CHECK (CategoryType <> N'Distance' OR DistanceKm IS NOT NULL),
    CONSTRAINT CK_Categories_DistanceKm         CHECK (DistanceKm IS NULL OR DistanceKm > 0),
    CONSTRAINT CK_Categories_MaxParticipants    CHECK (MaxParticipants IS NULL OR MaxParticipants > 0),
    CONSTRAINT FK_Categories_Events             FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE CASCADE
);
GO

PRINT 'STEP 5 complete: Categories created';
GO


-- =====================================================================
-- STEP 6: Enrolments - the bridge (associative) table that resolves the
--         many-to-many relationship between Participants and Events
-- =====================================================================
CREATE TABLE dbo.Enrolments
(
    EnrolmentId         INT             IDENTITY(1,1)  NOT NULL,
    ParticipantId       INT             NOT NULL,   -- the Participant (Users.UserId) who entered
    EventId             INT             NOT NULL,   -- kept so the database itself can block double entries
    CategoryId          INT             NOT NULL,   -- the category the Participant selected
    Status              NVARCHAR(20)    NOT NULL
        CONSTRAINT DF_Enrolments_Status DEFAULT (N'Pending'),
    EnrolledAt          DATETIME2       NOT NULL
        CONSTRAINT DF_Enrolments_EnrolledAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Enrolments                    PRIMARY KEY (EnrolmentId),
    CONSTRAINT UQ_Enrolments_Participant_Event  UNIQUE (ParticipantId, EventId),
    CONSTRAINT CK_Enrolments_Status             CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled')),
    CONSTRAINT FK_Enrolments_Participant        FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users (UserId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrolments_Events             FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE NO ACTION,
    CONSTRAINT FK_Enrolments_Categories         FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId) ON DELETE NO ACTION
);
GO

PRINT 'STEP 6 complete: Enrolments created';
GO


-- =====================================================================
-- STEP 7: Results - finish time and position for one enrolment
-- =====================================================================
CREATE TABLE dbo.Results
(
    ResultId            INT             IDENTITY(1,1)  NOT NULL,
    EnrolmentId         INT             NOT NULL,   -- UNIQUE: one result per enrolment (1 to 0..1)
    FinishTime          TIME(0)         NOT NULL,   -- hh:mm:ss, e.g. 01:21:37
    FinishPosition      INT             NOT NULL,   -- overall position, e.g. 47 (of 312 finishers)
    IsPublished         BIT             NOT NULL
        CONSTRAINT DF_Results_IsPublished DEFAULT (0),
    RecordedAt          DATETIME2       NOT NULL
        CONSTRAINT DF_Results_RecordedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Results                   PRIMARY KEY (ResultId),
    CONSTRAINT UQ_Results_EnrolmentId       UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_FinishTime        CHECK (FinishTime > '00:00:00'),
    CONSTRAINT CK_Results_FinishPosition    CHECK (FinishPosition > 0),
    CONSTRAINT FK_Results_Enrolments        FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments (EnrolmentId) ON DELETE CASCADE
);
GO

PRINT 'STEP 7 complete: Results created';
GO


-- =====================================================================
-- STEP 8: Indexes on foreign key columns
--   SQL Server does not index foreign keys automatically (EF Core does,
--   by convention, in Part 2). Categories.EventId, Enrolments.ParticipantId
--   and Results.EnrolmentId are already covered by their UNIQUE constraints.
-- =====================================================================
CREATE INDEX IX_Users_RoleId            ON dbo.Users (RoleId);
CREATE INDEX IX_Events_OrganiserId      ON dbo.Events (OrganiserId);
CREATE INDEX IX_Events_EventTypeId      ON dbo.Events (EventTypeId);
CREATE INDEX IX_Events_EventDate        ON dbo.Events (EventDate);      -- fast "upcoming events" lists
CREATE INDEX IX_Enrolments_EventId      ON dbo.Enrolments (EventId);
CREATE INDEX IX_Enrolments_CategoryId   ON dbo.Enrolments (CategoryId);
GO

PRINT 'STEP 8 complete: indexes created';
GO


-- =====================================================================
-- STEP 9: Describe every table and column inside SQL Server
--   Saved as MS_Description extended properties, so SSMS shows them
--   (right-click a column > Properties > Extended Properties) and CI can
--   check docs/Data_Dictionary.md against them. In Part 2, EF Core's
--   HasComment() creates exactly the same properties.
-- =====================================================================
DECLARE @Descriptions TABLE
(
    Id              INT             IDENTITY(1,1) PRIMARY KEY,
    TableName       SYSNAME         NOT NULL,
    ColumnName      SYSNAME         NULL,       -- NULL = describes the table itself
    Description     NVARCHAR(400)   NOT NULL
);

INSERT INTO @Descriptions (TableName, ColumnName, Description)
VALUES
    (N'Roles',      NULL,                   N'The two user roles RaceDay supports: Organiser and Participant.'),
    (N'Roles',      N'RoleId',              N'Primary key (identity).'),
    (N'Roles',      N'RoleName',            N'Role name: Organiser or Participant. Unique.'),

    (N'EventTypes', NULL,                   N'Kinds of event: Run, Walk or Cycle.'),
    (N'EventTypes', N'EventTypeId',         N'Primary key (identity).'),
    (N'EventTypes', N'TypeName',            N'Event type name: Run, Walk or Cycle. Unique.'),

    (N'Users',      NULL,                   N'Everyone who can log in. Organisers and Participants share this table and are told apart by RoleId.'),
    (N'Users',      N'UserId',              N'Primary key (identity).'),
    (N'Users',      N'RoleId',              N'Role of the user (FK to Roles). Chosen once at registration.'),
    (N'Users',      N'FirstName',           N'First name.'),
    (N'Users',      N'LastName',            N'Surname.'),
    (N'Users',      N'Email',               N'Login name. Unique, ignoring letter case, and must look like an email address.'),
    (N'Users',      N'PasswordHash',        N'BCrypt hash of the password. The plain-text password is never stored.'),
    (N'Users',      N'PhoneNumber',         N'Contact number (optional).'),
    (N'Users',      N'DateOfBirth',         N'Date of birth, used to check age categories. Must be in the past.'),
    (N'Users',      N'ProfilePictureUrl',   N'Azure Blob Storage URL of the profile picture (added in Part 3).'),
    (N'Users',      N'CreatedAt',           N'When the account was created (UTC).'),

    (N'Events',     NULL,                   N'A run, walk or cycle event, created and owned by one Organiser.'),
    (N'Events',     N'EventId',             N'Primary key (identity).'),
    (N'Events',     N'OrganiserId',         N'The Organiser who owns the event (FK to Users).'),
    (N'Events',     N'EventTypeId',         N'Run, Walk or Cycle (FK to EventTypes).'),
    (N'Events',     N'Name',                N'Event name shown to participants.'),
    (N'Events',     N'Description',         N'Full description of the event.'),
    (N'Events',     N'EventDate',           N'Race date and start time.'),
    (N'Events',     N'Location',            N'Venue or start location.'),
    (N'Events',     N'DistanceKm',          N'Main (longest) distance in km. Must be above 0.'),
    (N'Events',     N'BannerImageUrl',      N'Azure Blob Storage URL of the event banner (added in Part 3).'),
    (N'Events',     N'CreatedAt',           N'When the event was created (UTC).'),

    (N'Categories', NULL,                   N'An age or distance category that Participants choose when they enter an event.'),
    (N'Categories', N'CategoryId',          N'Primary key (identity).'),
    (N'Categories', N'EventId',             N'The event this category belongs to (FK to Events). Deleted with its event.'),
    (N'Categories', N'Name',                N'Category name, unique within its event.'),
    (N'Categories', N'CategoryType',        N'Age or Distance.'),
    (N'Categories', N'MinAge',              N'Youngest age allowed on race day (Age categories).'),
    (N'Categories', N'MaxAge',              N'Oldest age allowed on race day; empty means no upper limit (Age categories).'),
    (N'Categories', N'DistanceKm',          N'Distance in km (Distance categories). Must be above 0.'),
    (N'Categories', N'MaxParticipants',     N'Entry limit; empty means unlimited.'),

    (N'Enrolments', NULL,                   N'An entry by a Participant into an event in a chosen category. Resolves the many-to-many between Users and Events.'),
    (N'Enrolments', N'EnrolmentId',         N'Primary key (identity).'),
    (N'Enrolments', N'ParticipantId',       N'The Participant who entered (FK to Users).'),
    (N'Enrolments', N'EventId',             N'The event entered (FK to Events). One entry per participant per event.'),
    (N'Enrolments', N'CategoryId',          N'The category chosen (FK to Categories).'),
    (N'Enrolments', N'Status',              N'Pending, Confirmed or Cancelled. Starts as Pending.'),
    (N'Enrolments', N'EnrolledAt',          N'When the entry was made (UTC).'),

    (N'Results',    NULL,                   N'Finish time and position for one enrolment, captured by the Organiser after the event.'),
    (N'Results',    N'ResultId',            N'Primary key (identity).'),
    (N'Results',    N'EnrolmentId',         N'The entry this result belongs to (FK to Enrolments). One result per entry.'),
    (N'Results',    N'FinishTime',          N'Finish time as hh:mm:ss. Must be above zero.'),
    (N'Results',    N'FinishPosition',      N'Overall finishing position. Must be 1 or more.'),
    (N'Results',    N'IsPublished',         N'1 once the Organiser publishes the results; participants only see published results.'),
    (N'Results',    N'RecordedAt',          N'When the result was captured (UTC).');

DECLARE @i      INT = 1,
        @n      INT = (SELECT COUNT(*) FROM @Descriptions),
        @table  SYSNAME,
        @column SYSNAME,
        @text   NVARCHAR(400);

WHILE @i <= @n
BEGIN
    SELECT @table = TableName, @column = ColumnName, @text = Description
    FROM @Descriptions
    WHERE Id = @i;

    IF @column IS NULL
    BEGIN
        EXEC sys.sp_addextendedproperty
             @name = N'MS_Description', @value = @text,
             @level0type = N'SCHEMA', @level0name = N'dbo',
             @level1type = N'TABLE',  @level1name = @table;
    END
    ELSE
    BEGIN
        EXEC sys.sp_addextendedproperty
             @name = N'MS_Description', @value = @text,
             @level0type = N'SCHEMA', @level0name = N'dbo',
             @level1type = N'TABLE',  @level1name = @table,
             @level2type = N'COLUMN', @level2name = @column;
    END;

    SET @i += 1;
END;

PRINT CONCAT('STEP 9 complete: ', @n, ' table and column descriptions stored');
GO


-- =====================================================================
-- STEP 10: Seed data - roles, event types and users
--   IDENTITY_INSERT lets us use fixed IDs so the foreign keys below are
--   easy to follow. XACT_ABORT rolls everything back if any row fails.
-- =====================================================================
SET XACT_ABORT ON;
BEGIN TRANSACTION;

SET IDENTITY_INSERT dbo.Roles ON;
INSERT INTO dbo.Roles (RoleId, RoleName)
VALUES (1, N'Organiser'),
       (2, N'Participant');
SET IDENTITY_INSERT dbo.Roles OFF;

SET IDENTITY_INSERT dbo.EventTypes ON;
INSERT INTO dbo.EventTypes (EventTypeId, TypeName)
VALUES (1, N'Run'),
       (2, N'Walk'),
       (3, N'Cycle');
SET IDENTITY_INSERT dbo.EventTypes OFF;

-- Password for all demo users: RaceDay@2026 (BCrypt, cost 11)
SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users
    (UserId, RoleId, FirstName, LastName, Email, PasswordHash, PhoneNumber, DateOfBirth, ProfilePictureUrl, CreatedAt)
VALUES
    -- Organisers (RoleId 1)
    (1, 1, N'Thandi', N'Mokoena',       N'thandi.mokoena@example.com', N'$2a$11$wj6inT9j12Da9caXJQflmeMsmucCODikYIMnxXSBMGERv1jumu7Cu', N'082 555 0101', NULL,         NULL, '2026-05-04T08:30:00'),
    (2, 1, N'Pieter', N'van der Merwe', N'pieter.vdmerwe@example.com', N'$2a$11$NNCYCv1wLHPIUvg8MGnTN.HUU2OZOwV2Qd13VYpdil3oLF/rLv3fO', N'083 555 0102', NULL,         NULL, '2026-05-06T10:15:00'),
    -- Participants (RoleId 2)
    (3, 2, N'Sipho',  N'Ndlovu',        N'sipho.ndlovu@example.com',   N'$2a$11$dm1FMgHlLr0oA0VHvEKvPOU01M8Khxk.DgH38ey0QEcIV5DW6ZPim', N'071 555 0103', '1994-03-12', NULL, '2026-05-20T19:05:00'),
    (4, 2, N'Aisha',  N'Patel',         N'aisha.patel@example.com',    N'$2a$11$O1cENnzJQBo2uhqU.hST.uYtwy9wyjkkL7XSgu6owWjZoZtdwbNNe', N'072 555 0104', '2001-07-25', NULL, '2026-06-18T12:40:00'),
    (5, 2, N'Lerato', N'Dlamini',       N'lerato.dlamini@example.com', N'$2a$11$WBIcsPrlpT75IFs/QnTcPezlpOEfRgLdZfWw8g2iYkB2TIBBDL40K', N'073 555 0105', '1983-11-02', NULL, '2026-06-09T07:15:00'),
    (6, 2, N'Johan',  N'Botha',         N'johan.botha@example.com',    N'$2a$11$cS2gwsbPMHklOit.zMrDUeLVThMB7M6MzHjxiEkwG90NYGQ8ftfPe', N'074 555 0106', '1971-05-30', NULL, '2026-06-28T17:50:00'),
    (7, 2, N'Kabelo', N'Molefe',        N'kabelo.molefe@example.com',  N'$2a$11$VGLhem1qcFmVdgwc2ckbm.vyeF.j6Qeys3gKb6aNtcos5KXiTBc2O', N'076 555 0107', '2007-10-15', NULL, '2026-06-03T15:25:00');
SET IDENTITY_INSERT dbo.Users OFF;

COMMIT TRANSACTION;
PRINT 'STEP 10 complete: roles, event types and 7 users seeded';
GO


