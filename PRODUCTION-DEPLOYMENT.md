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

## ?? Fully Automated Deployment Process

### **Zero-Touch Deployment** ?

Your deployment is now **100% automated**! When you push to `main` or `master` branch:

1. **Build & Test** - Compiles and validates the application ?
2. **Publish** - Creates deployment artifacts ? 
3. **Deploy** - Uploads to FTP server with secure credentials ?
4. **Auto-Migrate** - Automatically applies database migrations via HTTP ?
5. **Verify** - Confirms migration status and application health ?

### **?? Complete Automation Benefits**

- **?? Zero Manual Steps** - Everything happens automatically
- **? Fast Deployment** - Migrations run immediately after upload
- **??? Secure** - Uses HTTP endpoint with token authentication
- **?? Status Reporting** - Real-time feedback on migration success
- **?? Health Monitoring** - Automatic verification of deployment

### **Deployment Flow**

```
GitHub Actions Pipeline (Fully Automated):
??? ?? Build & Test ?
??? ?? Publish Application ?
??? ?? Configure Database Credentials ?
??? ?? Upload to FTP Server ?
??? ? Wait for Application Startup ?
??? ?? Trigger Migrations via HTTP ?
??? ?? Verify Migration Status ?
??? ?? Deployment Complete ?

Total Time: ~3-5 minutes
Manual Steps Required: 0
```

### **Automatic Migration System**

Your application now includes a built-in HTTP migration endpoint:

**Migration Endpoint:** `POST /api/migration/apply`
- ? **Secure** - Requires authentication token
- ? **Automatic** - Triggered by GitHub Actions
- ? **Smart** - Only applies pending migrations
- ? **Logged** - Full migration details in application logs

**Status Endpoint:** `GET /api/migration/status`  
- ? **Real-time** - Current migration state
- ? **Detailed** - Shows applied and pending migrations
- ? **Accessible** - No authentication required for status

### **Required GitHub Secrets**

#### **Database Configuration:**
- `DB_SERVER` = `mart.zadns.co.za`
- `DB_USERNAME` = `[your database username]`
- `DB_PASSWORD` = `[your database password]`

#### **FTP Configuration:**
- `FTP_SERVER` = `[your FTP server hostname]`
- `FTP_USERNAME` = `[your FTP username]`
- `FTP_PASSWORD` = `[your FTP password]` (also used as migration auth token)

### **Development Workflow**

Creating new features is now extremely simple:

1. **Develop locally** with your changes
2. **Create migrations**:
   ```bash
   dotnet ef migrations add YourFeatureName --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
   ```
3. **Push to GitHub** - Everything else is automatic!
4. **Monitor deployment** in GitHub Actions
5. **Verify live application** - Ready in minutes!

### **Real-Time Deployment Monitoring**

GitHub Actions provides detailed feedback:

```
?? FULLY AUTOMATED DEPLOYMENT COMPLETED!

? What was accomplished:
   ?? Application built and tested
   ?? Database credentials configured securely  
   ?? Files uploaded to FTP server
   ?? Automatic migration completed successfully
   ?? Migration status verified - Database up to date!

?? Your application is live at:
   • https://your-server/gcweproperty.co.za/

?? Zero-touch deployment complete!
```

### **Application Endpoints**

After deployment, your application provides:

| Endpoint | Purpose | Authentication |
|----------|---------|----------------|
| `/` | Main application | User login |
| `/health` | Health check | None |
| `/api/migration/status` | Migration status | None |
| `/api/migration/apply` | Apply migrations | Token required |
| `/metrics` | Prometheus metrics | None |

### **Migration Monitoring**

Check migration status anytime:
```bash
curl https://your-server/gcweproperty.co.za/api/migration/status
```

Response example:
```json
{
  "connected": true,
  "appliedMigrations": 24,
  "pendingMigrations": 0,
  "lastAppliedMigration": "20241201_AddNewFeature",
  "isUpToDate": true
}
```

### **Security Features**

- ?? **Secure Credentials** - Database passwords never exposed in logs
- ??? **Token Authentication** - Migration endpoint requires valid auth token
- ?? **Audit Logging** - All migration attempts logged with IP addresses
- ?? **Unauthorized Protection** - Invalid requests are rejected and logged

### **Rollback Strategy**

If you need to rollback:

1. **Create rollback migration locally**:
   ```bash
   dotnet ef database update [PreviousMigrationName] --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
   ```
2. **Commit and push** - Automatic deployment applies rollback
3. **Verify** - Check migration status endpoint

### **Troubleshooting (Rare Cases)**

**If automatic migration fails (very unlikely):**

1. **Check GitHub Actions logs** for detailed error information
2. **Check application logs** on your server
3. **Manual trigger** (if needed):
   ```bash
   curl -X POST \
        -H "authToken: your-ftp-password" \
        https://your-server/gcweproperty.co.za/api/migration/apply
   ```

**Common Resolution Steps:**
- ? Verify database server connectivity
- ? Check database user permissions  
- ? Ensure .NET 8 runtime on server
- ? Confirm application started successfully

### **Performance & Reliability**

- **? Fast**: Complete deployment in 3-5 minutes
- **?? Reliable**: Multiple fallback mechanisms
- **?? Observable**: Real-time status and health monitoring
- **??? Safe**: Validates before applying changes
- **?? Recoverable**: Easy rollback if needed

### **What This Means For You**

?? **You can now deploy with a simple `git push`!**

No more:
- ? Manual FTP uploads
- ? Running migration scripts
- ? Database connection setup
- ? Checking deployment status

Just:
- ? Code your features
- ? Create migrations
- ? Push to GitHub
- ? Everything else happens automatically!

Your Property Management System now has **enterprise-grade CI/CD** with zero manual intervention required! ??