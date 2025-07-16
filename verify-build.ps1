Write-Host "Verifying build status..." -ForegroundColor Green

# Change to the project directory
Set-Location -Path $PSScriptRoot

# Build the entire solution
Write-Host "Building solution..." -ForegroundColor Yellow
$buildResult = dotnet build --no-restore --verbosity minimal

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful!" -ForegroundColor Green
    
    # Run a quick test to verify tests compile
    Write-Host "Running a quick test compilation check..." -ForegroundColor Yellow
    $testResult = dotnet test --no-build --verbosity minimal --collect:"XPlat Code Coverage" --logger:"console;verbosity=minimal"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Tests compile and run successfully!" -ForegroundColor Green
        Write-Host "✅ All issues have been resolved!" -ForegroundColor Green
    } else {
        Write-Host "❌ Tests failed to run" -ForegroundColor Red
        Write-Host "Check the output above for details" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    Write-Host "Check the output above for compilation errors" -ForegroundColor Red
}

Write-Host "Verification complete." -ForegroundColor Blue