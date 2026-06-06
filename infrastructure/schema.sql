-- Investment Tracker — manual schema script
-- Use this if EF Core auto-migration fails on startup.
-- Run against the RDS instance (investment-tracker-db).
-- Safe to re-run: all statements are idempotent.

-- Sequence for OfficeId (starts at 111, increments by 1)
IF NOT EXISTS (
    SELECT 1 FROM sys.sequences WHERE name = 'OfficeIdSequence' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE SEQUENCE dbo.OfficeIdSequence
        AS INT
        START WITH 111
        INCREMENT BY 1;
END;
GO

-- Main table
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'Offices' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE dbo.Offices (
        Id               INT            NOT NULL IDENTITY(1,1),
        OfficeId         INT            NOT NULL DEFAULT (NEXT VALUE FOR dbo.OfficeIdSequence),
        OfficeName       NVARCHAR(200)  NOT NULL,
        ManagerName      NVARCHAR(200)  NOT NULL,
        Email            NVARCHAR(255)  NOT NULL,
        Phone            NVARCHAR(20)   NOT NULL,
        UserName         NVARCHAR(100)  NOT NULL,
        PasswordHash     NVARCHAR(500)  NOT NULL,
        RegistrationDate DATETIME2      NOT NULL DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_Offices PRIMARY KEY (Id)
    );
END;
GO

-- Unique indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Offices_OfficeId' AND object_id = OBJECT_ID('dbo.Offices'))
    CREATE UNIQUE INDEX IX_Offices_OfficeId ON dbo.Offices (OfficeId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Offices_Phone' AND object_id = OBJECT_ID('dbo.Offices'))
    CREATE UNIQUE INDEX IX_Offices_Phone ON dbo.Offices (Phone);
GO

-- EF Core migrations history table (so EF treats the DB as already migrated)
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE dbo.__EFMigrationsHistory (
        MigrationId    NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
END;
GO

-- Mark InitialCreate as applied so EF Core does not try to run it again
IF NOT EXISTS (
    SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20260604000000_InitialCreate'
)
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260604000000_InitialCreate', '9.0.0');
END;
GO
