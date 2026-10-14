-- =====================================================================
-- AcxiomCRM Enterprise Database Setup & Initial Data Script
-- Database: Microsoft SQL Server (2019 / 2022 / Express / LocalDB / Azure)
-- =====================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AcxiomCRM')
BEGIN
    CREATE DATABASE [AcxiomCRM];
END
GO

USE [AcxiomCRM];
GO

-- 1. AspNetRoles
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetRoles')
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE NONCLUSTERED INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
END
GO

-- 2. AspNetUsers
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        [FullName] nvarchar(100) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE NONCLUSTERED INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
    CREATE NONCLUSTERED INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END
GO

-- 3. AspNetUserRoles
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUserRoles')
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END
GO

-- 4. Customers Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Customers')
BEGIN
    CREATE TABLE [Customers] (
        [CustomerId] int IDENTITY(1,1) NOT NULL,
        [CustomerCode] nvarchar(20) NOT NULL,
        [CustomerName] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [CompanyName] nvarchar(100) NOT NULL,
        [Address] nvarchar(250) NOT NULL,
        [City] nvarchar(50) NOT NULL,
        [State] nvarchar(50) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [OwnerId] nvarchar(450) NULL,
        [ModifiedDate] datetime2 NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId]),
        CONSTRAINT [FK_Customers_AspNetUsers_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Customers_CustomerCode] ON [Customers] ([CustomerCode]);
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Customers_Email] ON [Customers] ([Email]);
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Customers_Phone] ON [Customers] ([Phone]);
END
GO

-- 5. Leads Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Leads')
BEGIN
    CREATE TABLE [Leads] (
        [LeadId] int IDENTITY(1,1) NOT NULL,
        [LeadCode] nvarchar(20) NOT NULL,
        [LeadName] nvarchar(100) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [CompanyName] nvarchar(100) NOT NULL,
        [Source] nvarchar(50) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Priority] nvarchar(20) NOT NULL,
        [ExpectedValue] decimal(18,2) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [AssignedTo] nvarchar(450) NULL,
        [ConvertedCustomerId] int NULL,
        [ConvertedDate] datetime2 NULL,
        CONSTRAINT [PK_Leads] PRIMARY KEY ([LeadId]),
        CONSTRAINT [FK_Leads_AspNetUsers_AssignedTo] FOREIGN KEY ([AssignedTo]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Leads_Customers_ConvertedCustomerId] FOREIGN KEY ([ConvertedCustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Leads_LeadCode] ON [Leads] ([LeadCode]);
END
GO

-- 6. Opportunities Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Opportunities')
BEGIN
    CREATE TABLE [Opportunities] (
        [OpportunityId] int IDENTITY(1,1) NOT NULL,
        [OpportunityName] nvarchar(150) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Stage] nvarchar(30) NOT NULL,
        [Probability] int NOT NULL,
        [ExpectedCloseDate] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CustomerId] int NULL,
        [LeadId] int NULL,
        [OwnerId] nvarchar(450) NULL,
        [ClosedDate] datetime2 NULL,
        CONSTRAINT [PK_Opportunities] PRIMARY KEY ([OpportunityId]),
        CONSTRAINT [FK_Opportunities_AspNetUsers_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Opportunities_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Opportunities_Leads_LeadId] FOREIGN KEY ([LeadId]) REFERENCES [Leads] ([LeadId]) ON DELETE SET NULL
    );
END
GO

-- 7. FollowUps Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FollowUps')
BEGIN
    CREATE TABLE [FollowUps] (
        [FollowUpId] int IDENTITY(1,1) NOT NULL,
        [FollowUpType] nvarchar(30) NOT NULL,
        [ScheduledDate] datetime2 NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Notes] nvarchar(500) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CompletedDate] datetime2 NULL,
        [CustomerId] int NULL,
        [LeadId] int NULL,
        [OpportunityId] int NULL,
        [AssignedTo] nvarchar(450) NULL,
        [CreatedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_FollowUps] PRIMARY KEY ([FollowUpId]),
        CONSTRAINT [FK_FollowUps_AspNetUsers_AssignedTo] FOREIGN KEY ([AssignedTo]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_FollowUps_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE CASCADE,
        CONSTRAINT [FK_FollowUps_Leads_LeadId] FOREIGN KEY ([LeadId]) REFERENCES [Leads] ([LeadId]) ON DELETE CASCADE,
        CONSTRAINT [FK_FollowUps_Opportunities_OpportunityId] FOREIGN KEY ([OpportunityId]) REFERENCES [Opportunities] ([OpportunityId]) ON DELETE CASCADE
    );
END
GO

-- 8. Activities Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Activities')
BEGIN
    CREATE TABLE [Activities] (
        [ActivityId] int IDENTITY(1,1) NOT NULL,
        [ActivityType] nvarchar(30) NOT NULL,
        [Subject] nvarchar(150) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [ActivityDate] datetime2 NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CustomerId] int NULL,
        [LeadId] int NULL,
        [OpportunityId] int NULL,
        [UserId] nvarchar(450) NULL,
        CONSTRAINT [PK_Activities] PRIMARY KEY ([ActivityId]),
        CONSTRAINT [FK_Activities_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Activities_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Activities_Leads_LeadId] FOREIGN KEY ([LeadId]) REFERENCES [Leads] ([LeadId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Activities_Opportunities_OpportunityId] FOREIGN KEY ([OpportunityId]) REFERENCES [Opportunities] ([OpportunityId]) ON DELETE CASCADE
    );
END
GO

-- 9. AuditLogs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE [AuditLogs] (
        [AuditLogId] int IDENTITY(1,1) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        [Module] nvarchar(50) NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [EntityName] nvarchar(50) NULL,
        [RecordId] nvarchar(100) NULL,
        [Details] nvarchar(1000) NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [Result] nvarchar(20) NOT NULL,
        [UserId] nvarchar(450) NULL,
        [UserName] nvarchar(100) NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId])
    );
END
GO

-- =====================================================================
-- INITIAL SEED DATA
-- Note: When running the .NET project (dotnet run), DbInitializer.cs
-- automatically verifies and seeds all records, roles, and password hashes.
-- =====================================================================

PRINT 'AcxiomCRM SQL Server database schema verified successfully.';
GO
