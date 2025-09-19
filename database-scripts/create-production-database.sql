-- =============================================
-- Property Management Database Creation Script
-- Database: propertydb
-- Generated for Production Deployment
-- =============================================

USE [propertydb]
GO

-- Drop tables if they exist (for clean deployment)
IF OBJECT_ID(N'[dbo].[WaitingListNotifications]', N'U') IS NOT NULL
    DROP TABLE [dbo].[WaitingListNotifications]
GO

IF OBJECT_ID(N'[dbo].[WaitingListEntries]', N'U') IS NOT NULL
    DROP TABLE [dbo].[WaitingListEntries]
GO

IF OBJECT_ID(N'[dbo].[DigitalSignature]', N'U') IS NOT NULL
    DROP TABLE [dbo].[DigitalSignature]
GO

IF OBJECT_ID(N'[dbo].[LeaseTemplate]', N'U') IS NOT NULL
    DROP TABLE [dbo].[LeaseTemplate]
GO

IF OBJECT_ID(N'[dbo].[UtilityBills]', N'U') IS NOT NULL
    DROP TABLE [dbo].[UtilityBills]
GO

IF OBJECT_ID(N'[dbo].[Inspections]', N'U') IS NOT NULL
    DROP TABLE [dbo].[Inspections]
GO

IF OBJECT_ID(N'[dbo].[BookingRequests]', N'U') IS NOT NULL
    DROP TABLE [dbo].[BookingRequests]
GO

IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NOT NULL
    DROP TABLE [dbo].[Payments]
GO

IF OBJECT_ID(N'[dbo].[LeaseAgreements]', N'U') IS NOT NULL
    DROP TABLE [dbo].[LeaseAgreements]
GO

IF OBJECT_ID(N'[dbo].[MaintenanceRequests]', N'U') IS NOT NULL
    DROP TABLE [dbo].[MaintenanceRequests]
GO

IF OBJECT_ID(N'[dbo].[Tenants]', N'U') IS NOT NULL
    DROP TABLE [dbo].[Tenants]
GO

IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
    DROP TABLE [dbo].[Users]
GO

IF OBJECT_ID(N'[dbo].[Rooms]', N'U') IS NOT NULL
    DROP TABLE [dbo].[Rooms]
GO

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NOT NULL
    DROP TABLE [dbo].[__EFMigrationsHistory]
GO

-- Create EF Migrations History Table
CREATE TABLE [dbo].[__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
)
GO

-- Create Users Table
CREATE TABLE [dbo].[Users] (
    [UserId] int IDENTITY(1,1) NOT NULL,
    [Username] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
)
GO

-- Create Rooms Table
CREATE TABLE [dbo].[Rooms] (
    [RoomId] int IDENTITY(1,1) NOT NULL,
    [Number] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Floor] int NOT NULL,
    [Rent] decimal(18,2) NOT NULL,
    [Deposit] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Amenities] nvarchar(max) NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([RoomId])
)
GO

-- Create Tenants Table
CREATE TABLE [dbo].[Tenants] (
    [TenantId] int IDENTITY(1,1) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Contact] nvarchar(max) NOT NULL,
    [RoomId] int NOT NULL,
    [EmergencyContactName] nvarchar(max) NOT NULL,
    [EmergencyContactNumber] nvarchar(max) NOT NULL,
    [Username] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([TenantId]),
    CONSTRAINT [FK_Tenants_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenants_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
)
GO

-- Create MaintenanceRequests Table
CREATE TABLE [dbo].[MaintenanceRequests] (
    [MaintenanceId] int IDENTITY(1,1) NOT NULL,
    [RoomId] int NOT NULL,
    [Issue] nvarchar(max) NOT NULL,
    [DateReported] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Priority] nvarchar(max) NOT NULL,
    [AssignedTo] nvarchar(max) NULL,
    [DateCompleted] datetime2 NULL,
    [Cost] decimal(18,2) NULL,
    [Notes] nvarchar(max) NULL,
    CONSTRAINT [PK_MaintenanceRequests] PRIMARY KEY ([MaintenanceId]),
    CONSTRAINT [FK_MaintenanceRequests_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE CASCADE
)
GO

-- Create LeaseTemplate Table
CREATE TABLE [dbo].[LeaseTemplate] (
    [LeaseTemplateId] int IDENTITY(1,1) NOT NULL,
    [TemplateName] nvarchar(max) NOT NULL,
    [TemplateContent] nvarchar(max) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [Version] nvarchar(max) NULL,
    CONSTRAINT [PK_LeaseTemplate] PRIMARY KEY ([LeaseTemplateId])
)
GO

-- Create LeaseAgreements Table
CREATE TABLE [dbo].[LeaseAgreements] (
    [LeaseId] int IDENTITY(1,1) NOT NULL,
    [TenantId] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [RentAmount] decimal(18,2) NOT NULL,
    [DepositAmount] decimal(18,2) NOT NULL,
    [Terms] nvarchar(max) NULL,
    [ExpectedRentDay] int NOT NULL,
    [RoomId] int NOT NULL,
    [FilePath] nvarchar(max) NULL,
    [LeaseTemplateId] int NULL,
    CONSTRAINT [PK_LeaseAgreements] PRIMARY KEY ([LeaseId]),
    CONSTRAINT [FK_LeaseAgreements_LeaseTemplate_LeaseTemplateId] FOREIGN KEY ([LeaseTemplateId]) REFERENCES [LeaseTemplate] ([LeaseTemplateId]) ON DELETE SET NULL,
    CONSTRAINT [FK_LeaseAgreements_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LeaseAgreements_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([TenantId]) ON DELETE CASCADE
)
GO

-- Create DigitalSignature Table
CREATE TABLE [dbo].[DigitalSignature] (
    [DigitalSignatureId] int IDENTITY(1,1) NOT NULL,
    [LeaseAgreementId] int NOT NULL,
    [TenantId] int NOT NULL,
    [SignatureData] nvarchar(max) NOT NULL,
    [SignedDate] datetime2 NOT NULL,
    [IPAddress] nvarchar(max) NULL,
    [DeviceInfo] nvarchar(max) NULL,
    CONSTRAINT [PK_DigitalSignature] PRIMARY KEY ([DigitalSignatureId]),
    CONSTRAINT [FK_DigitalSignature_LeaseAgreements_LeaseAgreementId] FOREIGN KEY ([LeaseAgreementId]) REFERENCES [LeaseAgreements] ([LeaseId]) ON DELETE CASCADE,
    CONSTRAINT [FK_DigitalSignature_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([TenantId]) ON DELETE NO ACTION
)
GO

-- Create Payments Table
CREATE TABLE [dbo].[Payments] (
    [PaymentId] int IDENTITY(1,1) NOT NULL,
    [TenantId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PaymentMethod] nvarchar(max) NOT NULL,
    [Reference] nvarchar(max) NULL,
    [ProofOfPaymentPath] nvarchar(max) NULL,
    [PaymentMonth] int NOT NULL,
    [PaymentYear] int NOT NULL,
    [LeaseAgreementId] int NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [FK_Payments_LeaseAgreements_LeaseAgreementId] FOREIGN KEY ([LeaseAgreementId]) REFERENCES [LeaseAgreements] ([LeaseId]) ON DELETE SET NULL,
    CONSTRAINT [FK_Payments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([TenantId]) ON DELETE CASCADE
)
GO

-- Create BookingRequests Table
CREATE TABLE [dbo].[BookingRequests] (
    [BookingId] int IDENTITY(1,1) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Contact] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NULL,
    [RoomId] int NOT NULL,
    [PreferredMoveInDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [DateRequested] datetime2 NOT NULL,
    [Notes] nvarchar(max) NULL,
    [ProofOfPaymentPath] nvarchar(max) NULL,
    [Note] nvarchar(max) NULL,
    CONSTRAINT [PK_BookingRequests] PRIMARY KEY ([BookingId]),
    CONSTRAINT [FK_BookingRequests_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE CASCADE
)
GO

-- Create Inspections Table
CREATE TABLE [dbo].[Inspections] (
    [InspectionId] int IDENTITY(1,1) NOT NULL,
    [RoomId] int NOT NULL,
    [InspectionDate] datetime2 NOT NULL,
    [InspectorName] nvarchar(max) NOT NULL,
    [InspectionType] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NULL,
    [Issues] nvarchar(max) NULL,
    [PhotoPaths] nvarchar(max) NULL,
    [NextInspectionDate] datetime2 NULL,
    [Score] int NULL,
    CONSTRAINT [PK_Inspections] PRIMARY KEY ([InspectionId]),
    CONSTRAINT [FK_Inspections_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE CASCADE
)
GO

-- Create UtilityBills Table
CREATE TABLE [dbo].[UtilityBills] (
    [UtilityBillId] int IDENTITY(1,1) NOT NULL,
    [RoomId] int NOT NULL,
    [BillingMonth] int NOT NULL,
    [BillingYear] int NOT NULL,
    [WaterUsage] decimal(18,2) NOT NULL,
    [ElectricityUsage] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [DateCreated] datetime2 NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [IsPaid] bit NOT NULL,
    [PaidDate] datetime2 NULL,
    [Notes] nvarchar(max) NULL,
    CONSTRAINT [PK_UtilityBills] PRIMARY KEY ([UtilityBillId]),
    CONSTRAINT [FK_UtilityBills_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE CASCADE
)
GO

-- Create WaitingListEntries Table
CREATE TABLE [dbo].[WaitingListEntries] (
    [WaitingListId] int IDENTITY(1,1) NOT NULL,
    [PhoneNumber] nvarchar(15) NOT NULL,
    [FullName] nvarchar(100) NULL,
    [Email] nvarchar(100) NULL,
    [PreferredRoomType] nvarchar(20) NULL,
    [MaxBudget] decimal(18,2) NULL,
    [RequestDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Priority] int NOT NULL,
    [Notes] nvarchar(500) NULL,
    [Source] nvarchar(50) NULL,
    [LastContactDate] datetime2 NULL,
    CONSTRAINT [PK_WaitingListEntries] PRIMARY KEY ([WaitingListId])
)
GO

-- Create WaitingListNotifications Table
CREATE TABLE [dbo].[WaitingListNotifications] (
    [NotificationId] int IDENTITY(1,1) NOT NULL,
    [WaitingListId] int NOT NULL,
    [RoomId] int NULL,
    [MessageContent] nvarchar(1000) NOT NULL,
    [SentDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Response] nvarchar(100) NULL,
    [ResponseDate] datetime2 NULL,
    CONSTRAINT [PK_WaitingListNotifications] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK_WaitingListNotifications_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([RoomId]) ON DELETE SET NULL,
    CONSTRAINT [FK_WaitingListNotifications_WaitingListEntries_WaitingListId] FOREIGN KEY ([WaitingListId]) REFERENCES [WaitingListEntries] ([WaitingListId]) ON DELETE CASCADE
)
GO

-- Create Indexes
CREATE INDEX [IX_BookingRequests_RoomId] ON [dbo].[BookingRequests] ([RoomId])
GO

CREATE INDEX [IX_DigitalSignature_LeaseAgreementId] ON [dbo].[DigitalSignature] ([LeaseAgreementId])
GO

CREATE INDEX [IX_DigitalSignature_TenantId] ON [dbo].[DigitalSignature] ([TenantId])
GO

CREATE INDEX [IX_Inspections_RoomId] ON [dbo].[Inspections] ([RoomId])
GO

CREATE INDEX [IX_LeaseAgreements_LeaseTemplateId] ON [dbo].[LeaseAgreements] ([LeaseTemplateId])
GO

CREATE INDEX [IX_LeaseAgreements_RoomId] ON [dbo].[LeaseAgreements] ([RoomId])
GO

CREATE INDEX [IX_LeaseAgreements_TenantId] ON [dbo].[LeaseAgreements] ([TenantId])
GO

CREATE INDEX [IX_MaintenanceRequests_RoomId] ON [dbo].[MaintenanceRequests] ([RoomId])
GO

CREATE INDEX [IX_Payments_LeaseAgreementId] ON [dbo].[Payments] ([LeaseAgreementId])
GO

CREATE INDEX [IX_Payments_TenantId] ON [dbo].[Payments] ([TenantId])
GO

CREATE INDEX [IX_Tenants_RoomId] ON [dbo].[Tenants] ([RoomId])
GO

CREATE UNIQUE INDEX [IX_Tenants_UserId] ON [dbo].[Tenants] ([UserId])
GO

CREATE INDEX [IX_UtilityBills_RoomId] ON [dbo].[UtilityBills] ([RoomId])
GO

CREATE INDEX [IX_WaitingListNotifications_RoomId] ON [dbo].[WaitingListNotifications] ([RoomId])
GO

CREATE INDEX [IX_WaitingListNotifications_WaitingListId] ON [dbo].[WaitingListNotifications] ([WaitingListId])
GO

-- Insert Migration History Records
INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES
('20250615180546_InitialCreate', '8.0.0'),
('20250615183420_AddTenant', '8.0.0'),
('20250615195016_AddLeaseAgreement', '8.0.0'),
('20250616082755_AddPayment', '8.0.0'),
('20250616085926_SpecifyDecimalTypeForPaymentAmount', '8.0.0'),
('20250616095256_AddingTenantIdPayment', '8.0.0'),
('20250616103211_AddExpectedRentDayToLeaseAgreement', '8.0.0'),
('20250616105906_AddPaymentMonthYearToPayment', '8.0.0'),
('20250616122212_AddTenantPayments', '8.0.0'),
('20250616133617_AddTenantLeaseAgreements', '8.0.0'),
('20250616154738_UpdateTanentWithTenantId', '8.0.0'),
('20250616155559_EntitiesIdUpdate', '8.0.0'),
('20250616165947_AddProofOfPaymentToBookingRequest', '8.0.0'),
('20250616171709_AddNoteToBookingRequest', '8.0.0'),
('20250616173722_AddBookingRequest', '8.0.0'),
('20250617091858_AddEmergenciesContacts', '8.0.0'),
('20250617094724_AddSecurityLogin', '8.0.0'),
('20250617105131_AddRoomToLeaseAgreement', '8.0.0'),
('20250617131416_AddRoles', '8.0.0'),
('20250701125917_AddLeaseAgreementFilePath', '8.0.0'),
('20250711122312_AddingINspection', '8.0.0'),
('20250711132210_AddingBillTracking', '8.0.0'),
('20250806112610_AddingLeaseDigitalSignature', '8.0.0'),
('20250806113107_AddingLeaseDigitalSignature.2', '8.0.0'),
('20250808200655_AddWaitingListSystem', '8.0.0')
GO

-- Insert Initial Data (Admin User)
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [Role]) 
VALUES ('Admin', '$2a$11$E5bHQlY3FZ6Lk8FjI/R7HuGkFYJYfFczjnSgLnFvWYvhMcWgUQcdu', 'Manager')
GO

PRINT 'Database schema created successfully!'
PRINT 'Default admin user created:'
PRINT 'Username: Admin'
PRINT 'Password: 01Pa$$w0rd2025#'
GO