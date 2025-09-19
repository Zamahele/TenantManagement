# =============================================
# Manual Migration Script for Production
# Run this when you need to apply new migrations
# =============================================

param(
    [string]$ServerName = "AH-EPYC-3-SQL2019.zadns.co.za",
    [string]$DatabaseName = "propertydb",
    [string]$Username = "propertyadmin",
    [Parameter(Mandatory=$true)]
    [string]$Password
)

Write-Host "=======================================" -ForegroundColor Green
Write-Host "Manual Migration Application" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host "Server: $ServerName" -ForegroundColor Yellow
Write-Host "Database: $DatabaseName" -ForegroundColor Yellow
Write-Host "Username: $Username" -ForegroundColor Yellow
Write-Host ""

try {
    # Set environment for production
    $env:ASPNETCORE_ENVIRONMENT = "Production"
    
    # Build the application first
    Write-Host "Building application..." -ForegroundColor Cyan
    dotnet build --configuration Release
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    
    Write-Host "? Build successful!" -ForegroundColor Green
    
    # Check current migration status
    Write-Host "Checking current migration status..." -ForegroundColor Cyan
    $connectionString = "Server=$ServerName;Database=$DatabaseName;User Id=$Username;Password=$Password;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=True"
    
    dotnet ef migrations list --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web --connection $connectionString
    
    # Apply migrations
    Write-Host "Applying database migrations..." -ForegroundColor Cyan
    dotnet ef database update --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web --connection $connectionString --verbose
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "? Migrations applied successfully!" -ForegroundColor Green
        Write-Host ""
        
        # Show final migration status
        Write-Host "Final migration status:" -ForegroundColor Yellow
        dotnet ef migrations list --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web --connection $connectionString
        
    } else {
        throw "Migration failed"
    }
    
} catch {
    Write-Error "Migration process failed: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "1. Verify database server is accessible from this machine" -ForegroundColor White
    Write-Host "2. Check username and password are correct" -ForegroundColor White
    Write-Host "3. Ensure user has db_owner permissions" -ForegroundColor White
    Write-Host "4. Verify connection string format" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "=======================================" -ForegroundColor Green
Write-Host "MIGRATION COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green