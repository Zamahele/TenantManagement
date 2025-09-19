# =============================================
# Property Management Database Deployment Script
# Run this script to create the database schema on your production server
# =============================================

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$true)]
    [string]$DatabaseName = "propertydb",
    
    [Parameter(Mandatory=$true)]
    [string]$Username,
    
    [Parameter(Mandatory=$true)]
    [string]$Password
)

Write-Host "=======================================" -ForegroundColor Green
Write-Host "Property Management Database Deployment" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host "Server: $ServerName" -ForegroundColor Yellow
Write-Host "Database: $DatabaseName" -ForegroundColor Yellow
Write-Host "Username: $Username" -ForegroundColor Yellow
Write-Host ""

# Connection string
$connectionString = "Server=$ServerName;Database=master;User Id=$Username;Password=$Password;TrustServerCertificate=True;Encrypt=True"

try {
    # Test connection
    Write-Host "Testing connection to SQL Server..." -ForegroundColor Cyan
    $testConnection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $testConnection.Open()
    $testConnection.Close()
    Write-Host "? Connection successful!" -ForegroundColor Green
    
    # Create database if it doesn't exist
    Write-Host "Creating database '$DatabaseName' if it doesn't exist..." -ForegroundColor Cyan
    $createDbQuery = @"
        IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'$DatabaseName')
        BEGIN
            CREATE DATABASE [$DatabaseName]
            PRINT 'Database $DatabaseName created successfully!'
        END
        ELSE
        BEGIN
            PRINT 'Database $DatabaseName already exists.'
        END
"@
    
    Invoke-Sqlcmd -ConnectionString $connectionString -Query $createDbQuery
    Write-Host "? Database check completed!" -ForegroundColor Green
    
    # Run the schema creation script
    Write-Host "Running database schema creation script..." -ForegroundColor Cyan
    $scriptPath = Join-Path $PSScriptRoot "create-production-database.sql"
    
    if (Test-Path $scriptPath) {
        $newConnectionString = "Server=$ServerName;Database=$DatabaseName;User Id=$Username;Password=$Password;TrustServerCertificate=True;Encrypt=True"
        Invoke-Sqlcmd -ConnectionString $newConnectionString -InputFile $scriptPath
        Write-Host "? Database schema created successfully!" -ForegroundColor Green
    } else {
        Write-Error "Schema script not found at: $scriptPath"
        exit 1
    }
    
    Write-Host ""
    Write-Host "=======================================" -ForegroundColor Green
    Write-Host "DEPLOYMENT COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "=======================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Database Information:" -ForegroundColor Yellow
    Write-Host "- Server: $ServerName" -ForegroundColor White
    Write-Host "- Database: $DatabaseName" -ForegroundColor White
    Write-Host "- Username: $Username" -ForegroundColor White
    Write-Host ""
    Write-Host "Default Admin Login:" -ForegroundColor Yellow
    Write-Host "- Username: Admin" -ForegroundColor White
    Write-Host "- Password: 01Pa`$`$w0rd2025#" -ForegroundColor White
    Write-Host ""
    Write-Host "Connection String for Application:" -ForegroundColor Yellow
    Write-Host "Server=$ServerName;Database=$DatabaseName;User Id=$Username;Password=***;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=True" -ForegroundColor White

} catch {
    Write-Error "Database deployment failed: $($_.Exception.Message)"
    Write-Host "Full error details:" -ForegroundColor Red
    Write-Host $_.Exception.ToString() -ForegroundColor Red
    exit 1
}