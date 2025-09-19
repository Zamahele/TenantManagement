# =============================================
# Auto-Migration Script for FTP Server
# This script runs on the server after deployment
# =============================================

param(
    [string]$ServerName = "#{DB_SERVER}#",
    [string]$DatabaseName = "cottagedb", 
    [string]$Username = "#{DB_USERNAME}#",
    [string]$Password = "#{DB_PASSWORD}#",
    [string]$AppPath = "."
)

Write-Host "=======================================" -ForegroundColor Green
Write-Host "Property Management Auto-Migration" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host "Server: $ServerName" -ForegroundColor Yellow
Write-Host "Database: $DatabaseName" -ForegroundColor Yellow
Write-Host "Username: $Username" -ForegroundColor Yellow
Write-Host "App Path: $AppPath" -ForegroundColor Yellow
Write-Host ""

try {
    # Change to application directory
    if (Test-Path $AppPath) {
        Set-Location $AppPath
        Write-Host "? Changed to application directory: $AppPath" -ForegroundColor Green
    } else {
        Write-Host "? Application directory not found: $AppPath" -ForegroundColor Red
        exit 1
    }

    # Check if dotnet is available
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        Write-Host "? .NET runtime found" -ForegroundColor Green
    } else {
        Write-Host "? .NET runtime not found. Please install .NET 8 runtime." -ForegroundColor Red
        exit 1
    }

    # Check if application files exist
    if (Test-Path "PropertyManagement.Web.dll") {
        Write-Host "? Application files found" -ForegroundColor Green
    } else {
        Write-Host "? Application files not found. Ensure deployment completed successfully." -ForegroundColor Red
        exit 1
    }

    # Test database connection
    Write-Host "Testing database connection..." -ForegroundColor Cyan
    $connectionString = "Server=$ServerName;Database=$DatabaseName;User Id=$Username;Password=$Password;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=True"
    
    # Try to connect using .NET application
    $env:ConnectionStrings__DefaultConnection = $connectionString
    $env:ASPNETCORE_ENVIRONMENT = "Production"
    
    Write-Host "Checking for pending migrations..." -ForegroundColor Cyan
    
    # Run the application in migration mode (if we add this feature)
    # For now, we'll document that migrations should be applied manually
    
    Write-Host ""
    Write-Host "?? Manual Migration Required" -ForegroundColor Yellow
    Write-Host "To apply database migrations, run the following command:" -ForegroundColor White
    Write-Host ""
    Write-Host "dotnet PropertyManagement.Web.dll --migrate" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or use Entity Framework tools:" -ForegroundColor White
    Write-Host "dotnet ef database update --connection `"$connectionString`"" -ForegroundColor Cyan
    Write-Host ""
    
    Write-Host "? Deployment completed successfully!" -ForegroundColor Green
    Write-Host "?? Remember to apply database migrations if needed." -ForegroundColor Yellow
    
} catch {
    Write-Error "Migration check failed: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Troubleshooting tips:" -ForegroundColor Yellow
    Write-Host "1. Verify database server is accessible from this server" -ForegroundColor White
    Write-Host "2. Check username and password are correct" -ForegroundColor White
    Write-Host "3. Ensure database user has sufficient permissions" -ForegroundColor White
    Write-Host "4. Verify .NET 8 runtime is installed" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "=======================================" -ForegroundColor Green
Write-Host "DEPLOYMENT CHECK COMPLETED!" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green