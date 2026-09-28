/* =====================================================================
   RaceDay - Constraint tests for docs/RaceDay_Database.sql
   PROG6212 Programming 2B - POE Part 1
   Student : Ryan Naidoo (ST10487315)

   Run this AFTER RaceDay_Database.sql (open in SSMS and press F5).

   34 tests: 29 try to break a rule and 5 use a rule that should work.
   Every test runs inside a transaction that is always rolled back, so the
   seed data never changes and the script can be run as often as you like.
     BLOCK  passes only if SQL Server rejects the statement AND the error
            message names the expected constraint
     ALLOW  passes only if the statement works and its check sets @ok = 1
   The script ends with an error if any test fails, so CI goes red.
   ===================================================================== */
USE RaceDayDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT OFF;   -- let each failing statement be caught and rolled back on its own

DECLARE @Tests TABLE
(
    TestNo      INT             IDENTITY(1,1) PRIMARY KEY,
    TestName    NVARCHAR(200)   NOT NULL,
    Kind        NVARCHAR(5)     NOT NULL,   -- BLOCK or ALLOW
    Expected    NVARCHAR(100)   NOT NULL,   -- text the error must contain (BLOCK) or rule used (ALLOW)
    TestSql     NVARCHAR(MAX)   NOT NULL
);

INSERT INTO @Tests (TestName, Kind, Expected, TestSql)
VALUES
    -- Users
    (N'A second account cannot reuse an email address', N'BLOCK', N'UQ_Users_Email',
        N'INSERT INTO dbo.Users (RoleId, FirstName, LastName, Email, PasswordHash) VALUES (2, N''Copy'', N''Cat'', N''sipho.ndlovu@example.com'', N''hash'');'),
    (N'Email uniqueness ignores letter case', N'BLOCK', N'UQ_Users_Email',
        N'INSERT INTO dbo.Users (RoleId, FirstName, LastName, Email, PasswordHash) VALUES (2, N''Loud'', N''Sipho'', N''SIPHO.NDLOVU@EXAMPLE.COM'', N''hash'');'),
    (N'An email address must look like an email', N'BLOCK', N'CK_Users_Email',
        N'INSERT INTO dbo.Users (RoleId, FirstName, LastName, Email, PasswordHash) VALUES (2, N''No'', N''Email'', N''not-an-email'', N''hash'');'),
    (N'A date of birth cannot be in the future', N'BLOCK', N'CK_Users_DateOfBirth',
        N'INSERT INTO dbo.Users (RoleId, FirstName, LastName, Email, PasswordHash, DateOfBirth) VALUES (2, N''Future'', N''Kid'', N''future.kid@example.com'', N''hash'', ''2099-01-01'');'),
    (N'A user must have a real role', N'BLOCK', N'FK_Users_Roles',
        N'INSERT INTO dbo.Users (RoleId, FirstName, LastName, Email, PasswordHash) VALUES (99, N''Ghost'', N''Role'', N''ghost.role@example.com'', N''hash'');'),
    (N'A user must have a surname (NOT NULL)', N'BLOCK', N'column ''LastName''',
        N'INSERT INTO dbo.Users (RoleId, FirstName, LastName, Email, PasswordHash) VALUES (2, N''No'', NULL, N''no.surname@example.com'', N''hash'');'),
    -- Roles and event types
    (N'Only Organiser and Participant roles can exist', N'BLOCK', N'CK_Roles_RoleName',
        N'INSERT INTO dbo.Roles (RoleName) VALUES (N''Admin'');'),
    (N'A role that still has users cannot be deleted', N'BLOCK', N'FK_Users_Roles',
        N'DELETE FROM dbo.Roles WHERE RoleId = 2;'),
    (N'Event type names are unique', N'BLOCK', N'UQ_EventTypes_TypeName',
        N'INSERT INTO dbo.EventTypes (TypeName) VALUES (N''Run'');'),
    -- Events
    (N'An event distance must be above 0 km', N'BLOCK', N'CK_Events_DistanceKm',
        N'INSERT INTO dbo.Events (OrganiserId, EventTypeId, Name, Description, EventDate, Location, DistanceKm) VALUES (1, 1, N''Zero K'', N''Test'', ''2027-06-06T06:00:00'', N''Pretoria'', 0);'),
    (N'An event must have a real event type', N'BLOCK', N'FK_Events_EventTypes',
        N'INSERT INTO dbo.Events (OrganiserId, EventTypeId, Name, Description, EventDate, Location, DistanceKm) VALUES (1, 99, N''Mystery Race'', N''Test'', ''2027-06-06T06:00:00'', N''Pretoria'', 5);'),
    (N'An event must belong to a real user', N'BLOCK', N'FK_Events_Organiser',
        N'INSERT INTO dbo.Events (OrganiserId, EventTypeId, Name, Description, EventDate, Location, DistanceKm) VALUES (999, 1, N''Orphan Race'', N''Test'', ''2027-06-06T06:00:00'', N''Pretoria'', 5);'),
    (N'An event that has entries cannot be deleted', N'BLOCK', N'FK_Enrolments_',
        N'DELETE FROM dbo.Events WHERE EventId = 1;'),
    -- Categories
    (N'A category must be Age or Distance', N'BLOCK', N'CK_Categories_CategoryType',
        N'INSERT INTO dbo.Categories (EventId, Name, CategoryType) VALUES (2, N''Rainbow'', N''Colour'');'),
    (N'A minimum age cannot be above the maximum age', N'BLOCK', N'CK_Categories_AgeRange',
        N'INSERT INTO dbo.Categories (EventId, Name, CategoryType, MinAge, MaxAge) VALUES (4, N''Backwards'', N''Age'', 40, 30);'),
    (N'An Age category needs at least one age limit', N'BLOCK', N'CK_Categories_AgeNeedsLimit',
        N'INSERT INTO dbo.Categories (EventId, Name, CategoryType, MinAge, MaxAge) VALUES (4, N''Ageless'', N''Age'', NULL, NULL);'),
    (N'A Distance category needs a distance', N'BLOCK', N'CK_Categories_DistanceNeedsKm',
        N'INSERT INTO dbo.Categories (EventId, Name, CategoryType) VALUES (2, N''Mystery Ride'', N''Distance'');'),
    (N'A category distance must be above 0 km', N'BLOCK', N'CK_Categories_DistanceKm',
        N'INSERT INTO dbo.Categories (EventId, Name, CategoryType, DistanceKm) VALUES (2, N''Backwards Ride'', N''Distance'', -5);'),
    (N'An entry limit must be at least 1', N'BLOCK', N'CK_Categories_MaxParticipants',
        N'INSERT INTO dbo.Categories (EventId, Name, CategoryType, DistanceKm, MaxParticipants) VALUES (2, N''Nobody Ride'', N''Distance'', 20, 0);'),
    (N'Category names are unique within an event', N'BLOCK', N'UQ_Categories_Event_Name',
        N'INSERT INTO dbo.Categories (EventId, Name, CategoryType, DistanceKm) VALUES (2, N''30km Fun Ride'', N''Distance'', 30);'),
    (N'A category that has entries cannot be deleted', N'BLOCK', N'FK_Enrolments_Categories',
        N'DELETE FROM dbo.Categories WHERE CategoryId = 2;'),
    -- Enrolments
    (N'A participant cannot enter the same event twice', N'BLOCK', N'UQ_Enrolments_Participant_Event',
        N'INSERT INTO dbo.Enrolments (ParticipantId, EventId, CategoryId) VALUES (3, 1, 2);'),
    (N'An enrolment status must be Pending, Confirmed or Cancelled', N'BLOCK', N'CK_Enrolments_Status',
        N'UPDATE dbo.Enrolments SET Status = N''Maybe'' WHERE EnrolmentId = 1;'),
    (N'An enrolment must use a real category', N'BLOCK', N'FK_Enrolments_Categories',
        N'INSERT INTO dbo.Enrolments (ParticipantId, EventId, CategoryId) VALUES (4, 3, 999);'),
    (N'An enrolment must belong to a real participant', N'BLOCK', N'FK_Enrolments_Participant',
        N'INSERT INTO dbo.Enrolments (ParticipantId, EventId, CategoryId) VALUES (999, 3, 8);'),
    -- Results
    (N'An enrolment can only have one result', N'BLOCK', N'UQ_Results_EnrolmentId',
        N'INSERT INTO dbo.Results (EnrolmentId, FinishTime, FinishPosition) VALUES (1, ''01:30:00'', 9);'),
    (N'A finishing position must be 1 or more', N'BLOCK', N'CK_Results_FinishPosition',
        N'INSERT INTO dbo.Results (EnrolmentId, FinishTime, FinishPosition) VALUES (6, ''03:05:00'', 0);'),
    (N'A finish time must be above zero', N'BLOCK', N'CK_Results_FinishTime',
        N'INSERT INTO dbo.Results (EnrolmentId, FinishTime, FinishPosition) VALUES (6, ''00:00:00'', 1);'),
    (N'A result must belong to a real enrolment', N'BLOCK', N'FK_Results_Enrolments',
        N'INSERT INTO dbo.Results (EnrolmentId, FinishTime, FinishPosition) VALUES (999, ''01:00:00'', 1);'),
    -- Rules that must ALLOW an action
    (N'A new enrolment starts as Pending (DEFAULT)', N'ALLOW', N'DF_Enrolments_Status',
        N'INSERT INTO dbo.Enrolments (ParticipantId, EventId, CategoryId) VALUES (4, 3, 9); SELECT @ok = CASE WHEN Status = N''Pending'' AND EnrolledAt IS NOT NULL THEN 1 ELSE 0 END FROM dbo.Enrolments WHERE EnrolmentId = SCOPE_IDENTITY();'),
    (N'A new result starts unpublished (DEFAULT)', N'ALLOW', N'DF_Results_IsPublished',
        N'INSERT INTO dbo.Results (EnrolmentId, FinishTime, FinishPosition) VALUES (6, ''03:05:00'', 1); SELECT @ok = CASE WHEN IsPublished = 0 AND RecordedAt IS NOT NULL THEN 1 ELSE 0 END FROM dbo.Results WHERE ResultId = SCOPE_IDENTITY();'),
    (N'Deleting an event with no entries removes its categories (CASCADE)', N'ALLOW', N'FK_Categories_Events',
        N'INSERT INTO dbo.Events (OrganiserId, EventTypeId, Name, Description, EventDate, Location, DistanceKm) VALUES (1, 1, N''Temporary Event'', N''Test'', ''2027-06-06T06:00:00'', N''Pretoria'', 5); DECLARE @id INT = SCOPE_IDENTITY(); INSERT INTO dbo.Categories (EventId, Name, CategoryType, DistanceKm) VALUES (@id, N''5km'', N''Distance'', 5); DELETE FROM dbo.Events WHERE EventId = @id; SELECT @ok = CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE EventId = @id) THEN 1 ELSE 0 END;'),
    (N'Deleting an enrolment removes its result (CASCADE)', N'ALLOW', N'FK_Results_Enrolments',
        N'DELETE FROM dbo.Enrolments WHERE EnrolmentId = 1; SELECT @ok = CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.Results WHERE EnrolmentId = 1) THEN 1 ELSE 0 END;'),
    (N'The same category name can be used in a different event', N'ALLOW', N'UQ_Categories_Event_Name',
        N'INSERT INTO dbo.Categories (EventId, Name, CategoryType, DistanceKm) VALUES (3, N''30km Fun Ride'', N''Distance'', 30); SELECT @ok = 1;');

DECLARE @Results TABLE
(
    TestNo      INT             NOT NULL,
    Outcome     NVARCHAR(4)     NOT NULL,
    TestName    NVARCHAR(200)   NOT NULL,
    Expected    NVARCHAR(100)   NOT NULL,
    Detail      NVARCHAR(2048)  NOT NULL
);

DECLARE @i          INT = 1,
        @n          INT = (SELECT COUNT(*) FROM @Tests),
        @name       NVARCHAR(200),
        @kind       NVARCHAR(5),
        @expected   NVARCHAR(100),
        @sql        NVARCHAR(MAX),
        @ok         BIT,
        @error      NVARCHAR(2048),
        @outcome    NVARCHAR(4);

WHILE @i <= @n
BEGIN
    SELECT @name = TestName, @kind = Kind, @expected = Expected, @sql = TestSql
    FROM @Tests
    WHERE TestNo = @i;

    SET @ok = 0;
    SET @error = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC sys.sp_executesql @sql, N'@ok BIT OUTPUT', @ok = @ok OUTPUT;
        ROLLBACK TRANSACTION;   -- always undo, so the seed data never changes
    END TRY
    BEGIN CATCH
        SET @error = ERROR_MESSAGE();
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    END CATCH;

    SET @outcome = CASE
        WHEN @kind = N'BLOCK' AND @error LIKE N'%' + @expected + N'%'  THEN N'PASS'
        WHEN @kind = N'ALLOW' AND @error IS NULL AND @ok = 1          THEN N'PASS'
        ELSE N'FAIL'
    END;

    INSERT INTO @Results (TestNo, Outcome, TestName, Expected, Detail)
    VALUES (@i, @outcome, @name, @expected, COALESCE(@error, N'Statement ran and the check passed'));

    PRINT CONCAT(@outcome, N' | ', RIGHT(N'0' + CAST(@i AS NVARCHAR(3)), 2), N' | ', @name, N' | ', @expected);

    SET @i += 1;
END;

SELECT  TestNo      AS [#],
        Outcome,
        TestName    AS [Test],
        Expected    AS [Expected constraint or rule],
        Detail      AS [What SQL Server said]
FROM @Results
ORDER BY TestNo;

DECLARE @passed INT = (SELECT COUNT(*) FROM @Results WHERE Outcome = N'PASS');
PRINT CONCAT(N'Constraint tests: ', @passed, N' of ', @n, N' passed.');

IF @passed < @n
    THROW 50100, N'One or more constraint tests failed - see the results grid.', 1;
GO
