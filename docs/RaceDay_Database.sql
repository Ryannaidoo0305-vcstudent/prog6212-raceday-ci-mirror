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


