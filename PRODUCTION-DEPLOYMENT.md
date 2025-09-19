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
4. **FTP Deploy** - Replaces database credentials and uploads to FTP server
5. **Docker Build** - Creates and pushes Docker images

### ?? Automatic Database Migration

**Your application now automatically applies migrations on startup!** ?

When your application starts, it will:
- ? **Check for pending migrations** and apply them automatically
- ? **Create the admin user** if it doesn't exist
- ? **Apply seed data** if enabled in configuration
- ? **Log all database operations** for monitoring

### Migration Configuration

You can control migration behavior in `appsettings.json`:

```json
{
  "EnableAutoMigration": true,      // Auto-apply migrations on startup
  "EnableDatabaseSeeding": true     // Apply seed data
}
```

#### **Production Settings (Current):**
- `EnableAutoMigration`: `true` - ? Migrations apply automatically
- `EnableDatabaseSeeding`: `true` - ? Seed data is applied

#### **To Disable Auto-Migration:**
If you prefer manual control, set in your production config:
```json
{
  "EnableAutoMigration": false
}
```

### Database Migration Process

#### **Automatic (Recommended - Current Setup):**
? **No manual intervention required!**
- Deploy your application via GitHub Actions
- Application automatically applies any new migrations on startup
- Check application logs to verify migration status

#### **Manual (If auto-migration is disabled):**

**For New Database Setup:**
```powershell
.\database-scripts\deploy-database.ps1 -ServerName "AH-EPYC-3-SQL2019.zadns.co.za" -DatabaseName "propertydb" -Username "propertyadmin" -Password "YourPassword"
```

**For Schema Updates:**
```powershell
.\database-scripts\apply-migrations.ps1 -Password "YourPassword"
```

### Development Workflow

When you add new features that require database changes:

1. **Create migrations** in development:
   ```bash
   dotnet ef migrations add YourMigrationName --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
   ```
2. **Test locally** - migrations apply automatically when you run the app
3. **Push to GitHub** - triggers automatic deployment
4. **Migrations apply automatically** when the production app starts
5. **Monitor logs** to verify successful migration

### Migration Monitoring

Check application logs for migration status:

```bash
# Application will log:
? Found 2 pending migrations: AddNewFeature, UpdateUserTable
? Applying database migrations...
? Database migrations applied successfully!
? Database initialization completed successfully
?? Application startup completed

# Or if no migrations needed:
? Database is up to date - no pending migrations
```

### Rollback Strategy

If you need to rollback a migration:

1. **Disable auto-migration** temporarily:
   ```json
   { "EnableAutoMigration": false }
   ```
2. **Apply rollback manually**:
   ```bash
   dotnet ef database update [PreviousMigrationName] --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
   ```
3. **Re-enable auto-migration** after resolving issues

### Manual Migration Commands

```bash
# Check migration status
dotnet ef migrations list --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web

# Apply specific migration
dotnet ef database update [MigrationName] --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web

# Generate SQL script for review
dotnet ef migrations script --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web --output migration.sql

# Rollback to specific migration  
dotnet ef database update [PreviousMigrationName] --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
```

## ?? Configuration Details

### Production Connection String
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.com;Database=propertydb;User Id=propertymanagement_user;Password=YourSecurePassword123!;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=True"
  }
}
```

### Application Settings
- **Database**: `propertydb`
- **Seeding**: Enabled in production (creates default admin user)
- **Logging**: File-based logging to `/app/logs/`
- **Utilities**: Water (R0.02/L), Electricity (R1.50/kWh)

## ?? Default Admin Account

After deployment, use these credentials for initial login:
- **Username**: `Admin`
- **Password**: `01Pa$$w0rd2025#`

?? **Important**: Change the default admin password immediately after first login!

## ?? Verification

### Test Database Connection
```sql
-- Connect to your database and run:
SELECT COUNT(*) as TableCount 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';

-- Should return 12 tables
```

### Test Application
1. Access your deployed application URL
2. Login with admin credentials
3. Navigate through the dashboard
4. Create a test tenant/room to verify functionality

## ?? Troubleshooting

### Common Issues

**Database Connection Errors:**
- Verify server name, database name, username, and password
- Check firewall settings allow connections
- Ensure SQL Server is configured for SQL authentication

**Migration Failures:**
- Check if database user has sufficient permissions (db_owner role recommended)
- Verify connection string format and credentials
- Review GitHub Actions logs for specific migration errors
- Ensure database server allows remote connections

**FTP Deployment Issues:**
- Verify FTP credentials in GitHub secrets
- Check FTP server permissions for upload directory
- Ensure passive mode is supported

**Application Startup Errors:**
- Check application logs in `/app/logs/`
- Verify appsettings.Production.json has correct values
- Ensure all NuGet packages are properly deployed

### Support Commands

**Check deployment logs:**
```bash
# GitHub Actions logs
# Go to Actions tab in your repository

# Application logs (on server)
tail -f /app/logs/propertymanagement.log
```

**Database diagnostics:**
```sql
-- Check if migrations were applied
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;

-- Verify admin user exists
SELECT Username, Role FROM Users WHERE Role = 'Manager';

-- Check latest migration
SELECT TOP 1 MigrationId, ProductVersion 
FROM __EFMigrationsHistory 
ORDER BY MigrationId DESC;
```

**Migration troubleshooting:**
```bash
# Check EF tools version
dotnet ef --version

# Validate migration files
dotnet ef migrations list --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web

# Test connection string
dotnet ef database update --dry-run --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
```

## ?? Monitoring

The application includes:
- **Prometheus metrics** at `/metrics` endpoint
- **Structured logging** to files and console
- **Health checks** for database connectivity

## ?? Updates and Maintenance

### Updating the Application
1. Push changes to `main`/`master` branch
2. GitHub Actions automatically deploys
3. Database migrations run automatically if needed

### Database Backups
Set up regular backups of your `propertydb` database:
```sql
-- Example backup command (run on server)
BACKUP DATABASE [propertydb] 
TO DISK = 'C:\Backups\propertydb_backup.bak'
WITH FORMAT, COMPRESSION;
```

---

## ?? Support

For issues or questions:
1. Check the troubleshooting section above
2. Review GitHub Actions logs for deployment issues
3. Check application logs for runtime issues
4. Verify all secrets are correctly configured

Remember to keep your credentials secure and never commit them to source control!