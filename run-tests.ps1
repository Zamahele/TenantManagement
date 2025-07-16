Write-Host "Building solution..." -ForegroundColor Green
dotnet build --configuration Release

Write-Host "Running tests with coverage..." -ForegroundColor Green
dotnet test PropertyManagement.Test/PropertyManagement.Test.csproj `
  --configuration Release `
  --collect:"XPlat Code Coverage" `
  --results-directory:"TestResults" `
  --logger:"console;verbosity=detailed"

Write-Host "Installing ReportGenerator tool..." -ForegroundColor Green
dotnet tool install -g dotnet-reportgenerator-globaltool 2>$null

Write-Host "Generating coverage report..." -ForegroundColor Green
reportgenerator `
  -reports:"TestResults/*/coverage.cobertura.xml" `
  -targetdir:"TestResults/CoverageReport" `
  -reporttypes:"Html;Cobertura;TextSummary"

Write-Host "Coverage report generated in TestResults/CoverageReport/" -ForegroundColor Green

# Try to open the coverage report
$reportPath = "TestResults/CoverageReport/index.html"
if (Test-Path $reportPath) {
    Write-Host "Opening coverage report..." -ForegroundColor Green
    Start-Process $reportPath
} else {
    Write-Host "Coverage report not found at $reportPath" -ForegroundColor Red
}

Write-Host "Test execution completed!" -ForegroundColor Green