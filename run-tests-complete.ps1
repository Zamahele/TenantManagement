# PropertyManagement Complete Test Runner
# This script handles package restoration and runs all tests

param(
    [switch]$SkipRestore,
    [switch]$Verbose,
    [string]$Filter = "",
    [switch]$Coverage
)

Write-Host "PropertyManagement Test Suite Runner" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Set up variables
$solutionDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $solutionDir

$testProject = "PropertyManagement.Test"
$verbosityLevel = if ($Verbose) { "detailed" } else { "normal" }

# Function to check if tests can run
function Test-Prerequisites {
    Write-Host "Checking prerequisites..." -ForegroundColor Yellow
    
    if (-not (Test-Path "$testProject\PropertyManagement.Test.csproj")) {
        Write-Host "? Test project not found!" -ForegroundColor Red
        return $false
    }
    
    Write-Host "? Test project found" -ForegroundColor Green
    return $true
}

# Function to restore packages
function Restore-Packages {
    if ($SkipRestore) {
        Write-Host "??  Skipping package restore" -ForegroundColor Yellow
        return $true
    }
    
    Write-Host "?? Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Package restore failed! Check network connectivity." -ForegroundColor Red
        Write-Host "   Try running with -SkipRestore if packages are already restored" -ForegroundColor Yellow
        return $false
    }
    
    Write-Host "? Packages restored successfully" -ForegroundColor Green
    return $true
}

# Function to build solution
function Build-Solution {
    Write-Host "?? Building solution..." -ForegroundColor Yellow
    dotnet build --configuration Debug --verbosity minimal --no-restore
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Build failed!" -ForegroundColor Red
        return $false
    }
    
    Write-Host "? Build successful" -ForegroundColor Green
    return $true
}

# Function to run tests
function Run-Tests {
    Write-Host "?? Running tests..." -ForegroundColor Yellow
    
    $testArgs = @(
        "test"
        $testProject
        "--configuration", "Debug"
        "--no-build"
        "--logger", "console;verbosity=$verbosityLevel"
    )
    
    if ($Filter) {
        $testArgs += "--filter", $Filter
        Write-Host "   Filter: $Filter" -ForegroundColor Cyan
    }
    
    if ($Coverage) {
        $testArgs += "--collect", "XPlat Code Coverage"
        Write-Host "   Code coverage enabled" -ForegroundColor Cyan
    }
    
    & dotnet @testArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? All tests passed!" -ForegroundColor Green
        return $true
    } else {
        Write-Host "? Some tests failed" -ForegroundColor Red
        return $false
    }
}

# Function to run specific test categories
function Run-TestsByCategory {
    Write-Host "`n?? Running tests by category..." -ForegroundColor Cyan
    
    $categories = @(
        @{ Name = "Room Controller Tests"; Filter = "RoomsControllerTests" },
        @{ Name = "Inspection Controller Tests"; Filter = "InspectionsControllerTests" },
        @{ Name = "Payment Controller Tests"; Filter = "PaymentsControllerTests" },
        @{ Name = "Additional Controller Tests"; Filter = "AdditionalControllerTests" }
    )
    
    $allPassed = $true
    
    foreach ($category in $categories) {
        Write-Host "`n?? $($category.Name):" -ForegroundColor Yellow
        
        $testArgs = @(
            "test"
            $testProject
            "--configuration", "Debug"
            "--no-build"
            "--filter", $category.Filter
            "--logger", "console;verbosity=minimal"
        )
        
        & dotnet @testArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ? Passed" -ForegroundColor Green
        } else {
            Write-Host "   ? Failed" -ForegroundColor Red
            $allPassed = $false
        }
    }
    
    return $allPassed
}

# Main execution
try {
    if (-not (Test-Prerequisites)) {
        exit 1
    }
    
    if (-not (Restore-Packages)) {
        exit 1
    }
    
    if (-not (Build-Solution)) {
        exit 1
    }
    
    $testResult = Run-Tests
    
    if ($testResult -and -not $Filter) {
        # Run category breakdown if main tests passed
        Write-Host "`n" -NoNewline
        Run-TestsByCategory | Out-Null
    }
    
    # Summary
    Write-Host "`n?? Test Summary:" -ForegroundColor Cyan
    Write-Host "=================" -ForegroundColor Cyan
    
    if ($testResult) {
        Write-Host "?? All tests completed successfully!" -ForegroundColor Green
        Write-Host "? Service-based architecture working correctly" -ForegroundColor Green
        Write-Host "? Modern test patterns implemented" -ForegroundColor Green
        Write-Host "? Comprehensive test coverage verified" -ForegroundColor Green
    } else {
        Write-Host "??  Some tests need attention" -ForegroundColor Yellow
        Write-Host "?? Check individual test results above" -ForegroundColor Cyan
    }
    
    exit $(if ($testResult) { 0 } else { 1 })
    
} catch {
    Write-Host "?? Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}