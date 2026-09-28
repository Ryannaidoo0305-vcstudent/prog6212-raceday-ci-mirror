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


