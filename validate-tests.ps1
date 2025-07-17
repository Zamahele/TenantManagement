# Test Structure Validation Script
# This script validates the test project structure and key components

Write-Host "Validating PropertyManagement Test Structure..." -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

# Check if test files exist
$testFiles = @(
    "PropertyManagement.Test\Controllers\RoomsControllerTests.cs",
    "PropertyManagement.Test\Controllers\InspectionsControllerTests.cs", 
    "PropertyManagement.Test\Controllers\AdditionalControllerTests.cs",
    "PropertyManagement.Test\Controllers\PaymentsControllerTests.cs",
    "PropertyManagement.Test\TestValidation.cs"
)

Write-Host "Checking test files..." -ForegroundColor Yellow
foreach ($file in $testFiles) {
    if (Test-Path $file) {
        Write-Host "? $file" -ForegroundColor Green
    } else {
        Write-Host "? $file (Missing)" -ForegroundColor Red
    }
}

# Check test project file
Write-Host "`nChecking test project..." -ForegroundColor Yellow
if (Test-Path "PropertyManagement.Test\PropertyManagement.Test.csproj") {
    Write-Host "? PropertyManagement.Test.csproj exists" -ForegroundColor Green
} else {
    Write-Host "? PropertyManagement.Test.csproj missing" -ForegroundColor Red
}

# Analyze test content for service-based architecture
Write-Host "`nAnalyzing test architecture..." -ForegroundColor Yellow

$roomsTestContent = Get-Content "PropertyManagement.Test\Controllers\RoomsControllerTests.cs" -Raw
if ($roomsTestContent -match "IRoomApplicationService" -and $roomsTestContent -match "IBookingRequestApplicationService") {
    Write-Host "? RoomsControllerTests uses service-based architecture" -ForegroundColor Green
} else {
    Write-Host "? RoomsControllerTests may be using old repository pattern" -ForegroundColor Red
}

$inspectionsTestContent = Get-Content "PropertyManagement.Test\Controllers\InspectionsControllerTests.cs" -Raw
if ($inspectionsTestContent -match "IInspectionApplicationService" -and $inspectionsTestContent -match "Mock<IInspectionApplicationService>") {
    Write-Host "? InspectionsControllerTests uses service-based architecture" -ForegroundColor Green
} else {
    Write-Host "? InspectionsControllerTests may be using old repository pattern" -ForegroundColor Red
}

# Check for proper test patterns
Write-Host "`nValidating test patterns..." -ForegroundColor Yellow

if ($roomsTestContent -match "ServiceResult" -and $roomsTestContent -match "\.Setup.*\.ReturnsAsync") {
    Write-Host "? Tests use proper ServiceResult and async patterns" -ForegroundColor Green
} else {
    Write-Host "? Tests may not be using proper patterns" -ForegroundColor Red
}

if ($roomsTestContent -match "Mock.*Verify.*Times\.Once" -or $inspectionsTestContent -match "Mock.*Verify.*Times\.Once") {
    Write-Host "? Tests include proper verification" -ForegroundColor Green
} else {
    Write-Host "? Tests may be missing verification" -ForegroundColor Red
}

Write-Host "`nTest structure validation completed!" -ForegroundColor Cyan
Write-Host "Note: Network issues prevent running actual tests, but structure is validated." -ForegroundColor Yellow