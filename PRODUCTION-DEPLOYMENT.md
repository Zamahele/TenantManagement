# Property Management System - Production Deployment Guide

## Overview
This guide helps you deploy the Property Management System to production with proper database setup and secure credential management.

## ?? Prerequisites

1. **Production Database Server** with SQL Server
2. **FTP Server** access for web application deployment
3. **GitHub Repository** with Actions enabled

## ??? Database Setup

### Step 1: Create Database and User

1. **Connect to your SQL Server** as administrator
2. **Create database and user:**

```sql
-- Create the database
CREATE DATABASE [propertydb];
GO

-- Create a dedicated user for the application
USE [propertydb];
GO

CREATE LOGIN [propertymanagement_user] WITH PASSWORD = 'YourSecurePassword123!';
GO

CREATE USER [propertymanagement_user] FOR LOGIN [propertymanagement_user];
GO

-- Grant necessary permissions
ALTER ROLE [db_owner] ADD MEMBER [propertymanagement_user];
GO
```

### Step 2: Run Database Schema Script

**Option A: Using PowerShell Script (Recommended)**
```powershell
# Navigate to the database-scripts folder
cd database-scripts

# Run the deployment script
.\deploy-database.ps1 -ServerName "your-server.com" -DatabaseName "propertydb" -Username "propertymanagement_user" -Password "YourSecurePassword123!"
```

**Option B: Using SQL Server Management Studio**
1. Open `database-scripts/create-production-database.sql`
2. Connect to your server
3. Select the `propertydb` database
4. Execute the script

### Step 3: Verify Database Creation

The script creates these tables:
- `Users` (authentication)
- `Rooms` (property units)
- `Tenants` (tenant information)
- `LeaseAgreements` (lease contracts)
- `Payments` (payment tracking)
- `MaintenanceRequests` (maintenance workflow)
- `Inspections` (room inspections)
- `BookingRequests` (booking system)
- `UtilityBills` (utility tracking)
- `WaitingListEntries` & `WaitingListNotifications` (waiting list system)
- `LeaseTemplate` & `DigitalSignature` (digital lease management)

## ?? GitHub Secrets Configuration

Add these secrets to your GitHub repository:

### Database Secrets
- `DB_SERVER` - Your database server hostname (e.g., `your-server.database.windows.net`)
- `DB_USERNAME` - Database username (e.g., `propertymanagement_user`)
- `DB_PASSWORD` - Database password (e.g., `YourSecurePassword123!`)

### FTP Deployment Secrets
- `FTP_SERVER` - Your FTP server hostname
- `FTP_USERNAME` - FTP username
- `FTP_PASSWORD` - FTP password

### Other Secrets (if applicable)
- `CODECOV_TOKEN` - For code coverage reporting
- `GHCR_PAT` - GitHub Container Registry personal access token

### How to Add Secrets:
1. Go to your GitHub repository
2. Click **Settings** ? **Secrets and variables** ? **Actions**
3. Click **New repository secret**
4. Add each secret with its value

## ?? Deployment Process

### Automatic Deployment (GitHub Actions)

The deployment happens automatically when you push to `main` or `master` branch:

1. **Build** - Compiles the .NET application
2. **Test** - Runs unit tests with coverage
3. **Publish** - Creates deployment artifacts
4. **?? Database Migration** - **REQUIRED** - Applies Entity Framework migrations to production database
5. **FTP Deploy** - Replaces database credentials and uploads to FTP server (only if migrations succeed)
6. **Docker Build** - Creates and pushes Docker images

### ?? **Migration-Gated Deployment**

**Important:** Your deployment now uses a **migration-gated approach**:

- ? **Migrations MUST succeed** before application deployment
- ?? **If migrations fail**, FTP deployment is cancelled
- ??? **Prevents deploying incompatible code** to production
- ?? **Comprehensive error reporting** for troubleshooting

### Database Configuration

**Production Database:**
- **Server**: `mart.zadns.co.za`
- **Database**: `cottagedb`
- **Connection**: Secure with TLS encryption

### Required GitHub Secrets

You need to configure these secrets in your GitHub repository:

#### **Database Secrets:**
- `DB_SERVER` = `mart.zadns.co.za`
- `DB_USERNAME` = `[your database username]`
- `DB_PASSWORD` = `[your database password]`

#### **FTP Deployment Secrets:**
- `FTP_SERVER` = `[your FTP server]`
- `FTP_USERNAME` = `[your FTP username]`
- `FTP_PASSWORD` = `[your FTP password]`

### Development Workflow

When you add new features that require database changes:

1. **Create migrations** in development:
   ```bash
   dotnet ef migrations add YourMigrationName --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
   ```
2. **Test locally** with your development database
3. **Push to GitHub** - triggers automatic deployment pipeline
4. **Pipeline applies migrations** to production database automatically
5. **If migrations succeed** ? Application deploys to FTP server
6. **If migrations fail** ? Deployment stops with detailed error information

### Migration Pipeline Benefits

- ?? **Zero-downtime updates** - Database changes applied before app deployment
- ??? **Deployment safety** - Incompatible code cannot be deployed
- ?? **Full visibility** - Detailed logs of all migration activities
- ?? **Automatic rollback** - Failed deployments don't affect production

### Expected Pipeline Flow

```
? Build & Test Successful
? Publish Artifacts Created
?? Connecting to database: mart.zadns.co.za
? Database connection verified
?? Checking for pending migrations...
? Found 2 pending migrations: AddNewFeature, UpdateUserTable
?? Applying database migrations...
? Database migrations applied successfully!
? Deployment can proceed safely
?? FTP deployment started...
? Application deployed successfully!
```

### Troubleshooting Migration Failures

If migrations fail in the pipeline, check:

1. **Database Connectivity:**
   - Verify `mart.zadns.co.za` is accessible from GitHub Actions
   - Check firewall settings allow GitHub's IP ranges
   - Ensure database server accepts remote connections

2. **Credentials:**
   - Verify `DB_SERVER`, `DB_USERNAME`, `DB_PASSWORD` secrets are correct
   - Ensure database user has sufficient permissions (recommend `db_owner` role)

3. **Migration Issues:**
   - Review the detailed error logs in GitHub Actions
   - Test migrations locally first
   - Check for conflicting schema changes

### Manual Migration (If Pipeline Fails)

If you need to apply migrations manually:

```powershell
# Use the manual migration script
.\database-scripts\apply-migrations.ps1 -ServerName "mart.zadns.co.za" -DatabaseName "cottagedb" -Username "your_username" -Password "your_password"
```

### Rollback Strategy

If you need to rollback a migration:

1. **Create a rollback migration:**
   ```bash
   dotnet ef database update [PreviousMigrationName] --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
   ```
2. **Push the rollback** - pipeline will apply it automatically

### Production Safety Features

- **Connection Testing** - Verifies database connectivity before applying migrations
- **Detailed Logging** - Full migration output for debugging
- **Fail-Fast Deployment** - Stops deployment immediately on migration failure
- **Error Context** - Provides troubleshooting guidance when failures occur