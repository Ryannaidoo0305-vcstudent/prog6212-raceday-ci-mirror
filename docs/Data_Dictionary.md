# RaceDay - Data Dictionary

| | |
|---|---|
| **Module** | PROG6212 Programming 2B - POE Part 1 |
| **Student** | Ryan Naidoo (ST10487315) |
| **Source of truth** | `docs/RaceDay_Database.sql` - CI checks on every push that this dictionary, the ERD and the script agree |

Every table, column, relationship, index and named constraint in the RaceDay database. Each description is the same text the script stores inside SQL Server (STEP 9), so SSMS shows it too. All columns are `NOT NULL` unless the **Null** column says Yes.

## Contents

1. [Tables at a glance](#1-tables-at-a-glance)
2. [Tables and columns](#2-tables-and-columns)
3. [Relationships and delete rules](#3-relationships-and-delete-rules)
4. [Indexes](#4-indexes)
5. [All named constraints](#5-all-named-constraints)
6. [Rules the API enforces](#6-rules-the-api-enforces)

## 1. Tables at a glance

| Table | Purpose | Primary key | Columns | Rows seeded |
|---|---|---|---|---|
| [Roles](#roles) | The two user roles RaceDay supports: Organiser and Participant. | RoleId | 2 | 2 |
| [EventTypes](#eventtypes) | Kinds of event: Run, Walk or Cycle. | EventTypeId | 2 | 3 |
| [Users](#users) | Everyone who can log in. Organisers and Participants share this table and are told apart by RoleId. | UserId | 10 | 7 |
| [Events](#events) | A run, walk or cycle event, created and owned by one Organiser. | EventId | 10 | 4 |
| [Categories](#categories) | An age or distance category that Participants choose when they enter an event. | CategoryId | 8 | 13 |
| [Enrolments](#enrolments) | An entry by a Participant into an event in a chosen category. Resolves the many-to-many between Users and Events. | EnrolmentId | 6 | 10 |
| [Results](#results) | Finish time and position for one enrolment, captured by the Organiser after the event. | ResultId | 6 | 5 |

**Totals:** 7 tables, 44 columns, 40 named constraints, 6 indexes.

## 2. Tables and columns

### Roles

The two user roles RaceDay supports: Organiser and Participant.

| Column | Type | Null | Key | Default | Rules | Description |
|---|---|---|---|---|---|---|
| `RoleId` | INT | No | PK | IDENTITY(1,1) |  | Primary key (identity). |
| `RoleName` | NVARCHAR(20) | No | UQ |  | Unique; Organiser or Participant only | Role name: Organiser or Participant. Unique. |

### EventTypes

Kinds of event: Run, Walk or Cycle.

| Column | Type | Null | Key | Default | Rules | Description |
|---|---|---|---|---|---|---|
| `EventTypeId` | INT | No | PK | IDENTITY(1,1) |  | Primary key (identity). |
| `TypeName` | NVARCHAR(20) | No | UQ |  | Unique | Event type name: Run, Walk or Cycle. Unique. |

### Users

Everyone who can log in. Organisers and Participants share this table and are told apart by RoleId.

| Column | Type | Null | Key | Default | Rules | Description |
|---|---|---|---|---|---|---|
| `UserId` | INT | No | PK | IDENTITY(1,1) |  | Primary key (identity). |
| `RoleId` | INT | No | FK → Roles.RoleId |  | Must exist in Roles | Role of the user (FK to Roles). Chosen once at registration. |
| `FirstName` | NVARCHAR(50) | No |  |  |  | First name. |
| `LastName` | NVARCHAR(50) | No |  |  |  | Surname. |
| `Email` | NVARCHAR(256) | No | UQ |  | Unique (ignores letter case); Must look like an email address | Login name. Unique, ignoring letter case, and must look like an email address. |
| `PasswordHash` | NVARCHAR(255) | No |  |  |  | BCrypt hash of the password. The plain-text password is never stored. |
| `PhoneNumber` | NVARCHAR(20) | Yes |  |  |  | Contact number (optional). |
| `DateOfBirth` | DATE | Yes |  |  | From 1900-01-01 up to yesterday | Date of birth, used to check age categories. Must be in the past. |
| `ProfilePictureUrl` | NVARCHAR(500) | Yes |  |  |  | Azure Blob Storage URL of the profile picture (added in Part 3). |
| `CreatedAt` | DATETIME2 | No |  | `SYSUTCDATETIME()` |  | When the account was created (UTC). |

### Events

A run, walk or cycle event, created and owned by one Organiser.

| Column | Type | Null | Key | Default | Rules | Description |
|---|---|---|---|---|---|---|
| `EventId` | INT | No | PK | IDENTITY(1,1) |  | Primary key (identity). |
| `OrganiserId` | INT | No | FK → Users.UserId |  | Must exist in Users | The Organiser who owns the event (FK to Users). |
| `EventTypeId` | INT | No | FK → EventTypes.EventTypeId |  | Must exist in EventTypes | Run, Walk or Cycle (FK to EventTypes). |
| `Name` | NVARCHAR(150) | No |  |  |  | Event name shown to participants. |
| `Description` | NVARCHAR(2000) | No |  |  |  | Full description of the event. |
| `EventDate` | DATETIME2 | No |  |  |  | Race date and start time. |
| `Location` | NVARCHAR(200) | No |  |  |  | Venue or start location. |
| `DistanceKm` | DECIMAL(6,2) | No |  |  | Above 0 | Main (longest) distance in km. Must be above 0. |
| `BannerImageUrl` | NVARCHAR(500) | Yes |  |  |  | Azure Blob Storage URL of the event banner (added in Part 3). |
| `CreatedAt` | DATETIME2 | No |  | `SYSUTCDATETIME()` |  | When the event was created (UTC). |

### Categories

An age or distance category that Participants choose when they enter an event.

| Column | Type | Null | Key | Default | Rules | Description |
|---|---|---|---|---|---|---|
| `CategoryId` | INT | No | PK | IDENTITY(1,1) |  | Primary key (identity). |
| `EventId` | INT | No | FK → Events.EventId, UQ* |  | Must exist in Events; Unique together with Name | The event this category belongs to (FK to Events). Deleted with its event. |
| `Name` | NVARCHAR(100) | No | UQ* |  | Unique together with EventId | Category name, unique within its event. |
| `CategoryType` | NVARCHAR(10) | No |  |  | Age or Distance | Age or Distance. |
| `MinAge` | INT | Yes |  |  | MinAge cannot be above MaxAge; Age categories need MinAge or MaxAge | Youngest age allowed on race day (Age categories). |
| `MaxAge` | INT | Yes |  |  | MinAge cannot be above MaxAge; Age categories need MinAge or MaxAge | Oldest age allowed on race day; empty means no upper limit (Age categories). |
| `DistanceKm` | DECIMAL(6,2) | Yes |  |  | Required for Distance categories; Above 0 | Distance in km (Distance categories). Must be above 0. |
| `MaxParticipants` | INT | Yes |  |  | 1 or more | Entry limit; empty means unlimited. |

### Enrolments

An entry by a Participant into an event in a chosen category. Resolves the many-to-many between Users and Events.

| Column | Type | Null | Key | Default | Rules | Description |
|---|---|---|---|---|---|---|
| `EnrolmentId` | INT | No | PK | IDENTITY(1,1) |  | Primary key (identity). |
| `ParticipantId` | INT | No | FK → Users.UserId, UQ* |  | Must exist in Users; Unique together with EventId | The Participant who entered (FK to Users). |
| `EventId` | INT | No | FK → Events.EventId, UQ* |  | Must exist in Events; Unique together with ParticipantId | The event entered (FK to Events). One entry per participant per event. |
| `CategoryId` | INT | No | FK → Categories.CategoryId |  | Must exist in Categories | The category chosen (FK to Categories). |
| `Status` | NVARCHAR(20) | No |  | `N'Pending'` | Pending, Confirmed or Cancelled | Pending, Confirmed or Cancelled. Starts as Pending. |
| `EnrolledAt` | DATETIME2 | No |  | `SYSUTCDATETIME()` |  | When the entry was made (UTC). |

### Results

Finish time and position for one enrolment, captured by the Organiser after the event.

| Column | Type | Null | Key | Default | Rules | Description |
|---|---|---|---|---|---|---|
| `ResultId` | INT | No | PK | IDENTITY(1,1) |  | Primary key (identity). |
| `EnrolmentId` | INT | No | FK → Enrolments.EnrolmentId, UQ |  | Must exist in Enrolments; Unique | The entry this result belongs to (FK to Enrolments). One result per entry. |
| `FinishTime` | TIME(0) | No |  |  | Above 00:00:00 | Finish time as hh:mm:ss. Must be above zero. |
| `FinishPosition` | INT | No |  |  | 1 or more | Overall finishing position. Must be 1 or more. |
| `IsPublished` | BIT | No |  | `0` |  | 1 once the Organiser publishes the results; participants only see published results. |
| `RecordedAt` | DATETIME2 | No |  | `SYSUTCDATETIME()` |  | When the result was captured (UTC). |

`UQ*` = part of a unique constraint that covers more than one column.

## 3. Relationships and delete rules

| Relationship | Foreign key | Cardinality | On delete | Why |
|---|---|---|---|---|
| Roles → Users | `Users.RoleId` (FK_Users_Roles) | 1 to 0..* | NO ACTION | A role that is in use can never disappear |
| Users (Organiser) → Events | `Events.OrganiserId` (FK_Events_Organiser) | 1 to 0..* | NO ACTION | Deleting an account can never silently delete events |
| EventTypes → Events | `Events.EventTypeId` (FK_Events_EventTypes) | 1 to 0..* | NO ACTION | An event type in use can never disappear |
| Events → Categories | `Categories.EventId` (FK_Categories_Events) | 1 to 0..* | CASCADE | Categories mean nothing without their event, so they go with it |
| Users (Participant) → Enrolments | `Enrolments.ParticipantId` (FK_Enrolments_Participant) | 1 to 0..* | NO ACTION | Entries and results are race history, so they are kept |
| Events → Enrolments | `Enrolments.EventId` (FK_Enrolments_Events) | 1 to 0..* | NO ACTION | An event with entries cannot be deleted by accident (API returns 409) |
| Categories → Enrolments | `Enrolments.CategoryId` (FK_Enrolments_Categories) | 1 to 0..* | NO ACTION | A category with entries cannot be deleted (API returns 409) |
| Enrolments → Results | `Results.EnrolmentId` (FK_Results_Enrolments) | 1 to 0..1 | CASCADE | A result only makes sense with its entry, so it goes with it |
| Users (Participant) ↔ Events | through `Enrolments` | many to many | - | A participant enters many events and an event has many participants, so the Enrolments associative table sits between them |

Only two relationships cascade, and their paths never overlap. That keeps deletes predictable and avoids SQL Server's "may cause cycles or multiple cascade paths" error (Msg 1785), which EF Core's default cascade rules would otherwise trigger in Part 2.

## 4. Indexes

| Index | Table | Columns | Why |
|---|---|---|---|
| IX_Users_RoleId | Users | RoleId | Find users by role; supports FK_Users_Roles checks |
| IX_Events_OrganiserId | Events | OrganiserId | Organiser dashboard - an organiser's own events (#10) |
| IX_Events_EventTypeId | Events | EventTypeId | Filter events by Run, Walk or Cycle (#8) |
| IX_Events_EventDate | Events | EventDate | Upcoming events, soonest first (#8) |
| IX_Enrolments_EventId | Enrolments | EventId | Entries per event and dashboard counts (#10, #22) |
| IX_Enrolments_CategoryId | Enrolments | CategoryId | Spaces left per category (#15, #19) |

Foreign keys that are the first column of a unique constraint are already indexed by it: `Categories.EventId` (UQ_Categories_Event_Name), `Enrolments.ParticipantId` (UQ_Enrolments_Participant_Event) and `Results.EnrolmentId` (UQ_Results_EnrolmentId).

## 5. All named constraints

| Constraint | Kind | Table | Definition |
|---|---|---|---|
| PK_Roles | Primary key | Roles | (RoleId) |
| UQ_Roles_RoleName | Unique | Roles | (RoleName) |
| CK_Roles_RoleName | Check | Roles | `RoleName IN (N'Organiser', N'Participant')` |
| PK_EventTypes | Primary key | EventTypes | (EventTypeId) |
| UQ_EventTypes_TypeName | Unique | EventTypes | (TypeName) |
| PK_Users | Primary key | Users | (UserId) |
| FK_Users_Roles | Foreign key | Users | RoleId → Roles.RoleId, ON DELETE NO ACTION |
| UQ_Users_Email | Unique | Users | (Email) |
| CK_Users_Email | Check | Users | `Email LIKE N'%_@_%._%'` |
| CK_Users_DateOfBirth | Check | Users | `DateOfBirth IS NULL OR (DateOfBirth >= '1900-01-01' AND DateOfBirth < CAST(SYSUTCDATETIME() AS DATE))` |
| DF_Users_CreatedAt | Default | Users | CreatedAt = `SYSUTCDATETIME()` |
| PK_Events | Primary key | Events | (EventId) |
| FK_Events_Organiser | Foreign key | Events | OrganiserId → Users.UserId, ON DELETE NO ACTION |
| FK_Events_EventTypes | Foreign key | Events | EventTypeId → EventTypes.EventTypeId, ON DELETE NO ACTION |
| CK_Events_DistanceKm | Check | Events | `DistanceKm > 0` |
| DF_Events_CreatedAt | Default | Events | CreatedAt = `SYSUTCDATETIME()` |
| PK_Categories | Primary key | Categories | (CategoryId) |
| FK_Categories_Events | Foreign key | Categories | EventId → Events.EventId, ON DELETE CASCADE |
| UQ_Categories_Event_Name | Unique | Categories | (EventId, Name) |
| CK_Categories_CategoryType | Check | Categories | `CategoryType IN (N'Age', N'Distance')` |
| CK_Categories_AgeRange | Check | Categories | `MinAge IS NULL OR MaxAge IS NULL OR MinAge <= MaxAge` |
| CK_Categories_AgeNeedsLimit | Check | Categories | `CategoryType <> N'Age' OR MinAge IS NOT NULL OR MaxAge IS NOT NULL` |
| CK_Categories_DistanceNeedsKm | Check | Categories | `CategoryType <> N'Distance' OR DistanceKm IS NOT NULL` |
| CK_Categories_DistanceKm | Check | Categories | `DistanceKm IS NULL OR DistanceKm > 0` |
| CK_Categories_MaxParticipants | Check | Categories | `MaxParticipants IS NULL OR MaxParticipants > 0` |
| PK_Enrolments | Primary key | Enrolments | (EnrolmentId) |
| FK_Enrolments_Participant | Foreign key | Enrolments | ParticipantId → Users.UserId, ON DELETE NO ACTION |
| FK_Enrolments_Events | Foreign key | Enrolments | EventId → Events.EventId, ON DELETE NO ACTION |
| FK_Enrolments_Categories | Foreign key | Enrolments | CategoryId → Categories.CategoryId, ON DELETE NO ACTION |
| UQ_Enrolments_Participant_Event | Unique | Enrolments | (ParticipantId, EventId) |
| CK_Enrolments_Status | Check | Enrolments | `Status IN (N'Pending', N'Confirmed', N'Cancelled')` |
| DF_Enrolments_Status | Default | Enrolments | Status = `N'Pending'` |
| DF_Enrolments_EnrolledAt | Default | Enrolments | EnrolledAt = `SYSUTCDATETIME()` |
| PK_Results | Primary key | Results | (ResultId) |
| FK_Results_Enrolments | Foreign key | Results | EnrolmentId → Enrolments.EnrolmentId, ON DELETE CASCADE |
| UQ_Results_EnrolmentId | Unique | Results | (EnrolmentId) |
| CK_Results_FinishTime | Check | Results | `FinishTime > '00:00:00'` |
| CK_Results_FinishPosition | Check | Results | `FinishPosition > 0` |
| DF_Results_IsPublished | Default | Results | IsPublished = `0` |
| DF_Results_RecordedAt | Default | Results | RecordedAt = `SYSUTCDATETIME()` |

All 40 constraints are tested in `docs/RaceDay_Constraint_Tests.sql`, which CI runs on every push.

## 6. Rules the API enforces

Some rules need information from more than one row or table, so the API checks them in Part 2 (see the business rules in `API_Endpoint_Plan.md`):

| Rule | Why the database alone can't enforce it | Endpoint |
|---|---|---|
| `Events.OrganiserId` must be an Organiser, and `Enrolments.ParticipantId` a Participant | A foreign key can check that the user exists, not what role they have | #11, #19 |
| The chosen category must belong to the event being entered | Needs `Categories.EventId` compared with `Enrolments.EventId` | #19 |
| The participant's age on race day must fit the category | Needs `Users.DateOfBirth`, `Events.EventDate` and the category limits together | #19 |
| A category cannot take more entries than `MaxParticipants` | Needs a count of the category's non-cancelled entries | #19 |
| Results only after the event, only for Confirmed entries, one position per finisher | Needs the event date, the entry status and the event's other results | #24, #25 |
| New events must be in the future | Past events are valid data (they hold results), so only new ones are checked | #11, #12 |
