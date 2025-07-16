Write-Host "Analyzing potential test issues..." -ForegroundColor Yellow

# Check for common test issues
Write-Host "1. Checking for async/await patterns..." -ForegroundColor Cyan
$asyncIssues = Select-String -Path "PropertyManagement.Test\**\*.cs" -Pattern "async.*void|\.Result|\.Wait\(\)" -AllMatches
if ($asyncIssues.Count -gt 0) {
    Write-Host "   ⚠️  Found potential async issues:" -ForegroundColor Yellow
    $asyncIssues | ForEach-Object { Write-Host "     $($_.Filename):$($_.LineNumber) - $($_.Line.Trim())" }
} else {
    Write-Host "   ✅ No async/await issues found" -ForegroundColor Green
}

Write-Host "2. Checking for null reference patterns..." -ForegroundColor Cyan
$nullIssues = Select-String -Path "PropertyManagement.Test\**\*.cs" -Pattern "\.Object\s*\?\." -AllMatches
if ($nullIssues.Count -gt 0) {
    Write-Host "   ⚠️  Found potential null reference issues:" -ForegroundColor Yellow
    $nullIssues | ForEach-Object { Write-Host "     $($_.Filename):$($_.LineNumber) - $($_.Line.Trim())" }
} else {
    Write-Host "   ✅ No obvious null reference issues found" -ForegroundColor Green
}

Write-Host "3. Checking for missing using statements..." -ForegroundColor Cyan
$missingUsings = Select-String -Path "PropertyManagement.Test\**\*.cs" -Pattern "using.*;" | Group-Object Filename | Where-Object { $_.Count -lt 5 }
if ($missingUsings.Count -gt 0) {
    Write-Host "   ⚠️  Files with few using statements (might be missing imports):" -ForegroundColor Yellow
    $missingUsings | ForEach-Object { Write-Host "     $($_.Name) - $($_.Count) using statements" }
} else {
    Write-Host "   ✅ All test files have adequate using statements" -ForegroundColor Green
}

Write-Host "4. Checking for test method naming..." -ForegroundColor Cyan
$testMethods = Select-String -Path "PropertyManagement.Test\**\*.cs" -Pattern "\[Fact\]" -AllMatches
$testMethodsCount = $testMethods.Count
Write-Host "   ✅ Found $testMethodsCount test methods" -ForegroundColor Green

Write-Host "5. Most likely test results:" -ForegroundColor Cyan
Write-Host "   📊 Expected: Most tests should pass" -ForegroundColor Green
Write-Host "   📊 Possible issues: ViewData assertions, async timing, mock setup" -ForegroundColor Yellow
Write-Host "   📊 Recommendation: Run tests with detailed output for specific failures" -ForegroundColor Blue

Write-Host "`nTo run tests and see detailed output:" -ForegroundColor White
Write-Host "   dotnet test --logger `"console;verbosity=detailed`"" -ForegroundColor Gray
Write-Host "   dotnet test --logger `"console;verbosity=detailed`" --filter `"FullyQualifiedName~MaintenanceController`"" -ForegroundColor Gray