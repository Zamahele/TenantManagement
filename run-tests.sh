#!/bin/bash

echo "Building solution..."
dotnet build --configuration Release

echo "Running tests with coverage..."
dotnet test PropertyManagement.Test/PropertyManagement.Test.csproj \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory:"TestResults" \
  --logger:"console;verbosity=detailed"

echo "Generating coverage report..."
dotnet tool install -g dotnet-reportgenerator-globaltool 2>/dev/null || true

reportgenerator \
  -reports:"TestResults/*/coverage.cobertura.xml" \
  -targetdir:"TestResults/CoverageReport" \
  -reporttypes:"Html;Cobertura;TextSummary"

echo "Coverage report generated in TestResults/CoverageReport/"
echo "Opening coverage report..."

# Try to open the coverage report
if command -v xdg-open > /dev/null; then
  xdg-open TestResults/CoverageReport/index.html
elif command -v open > /dev/null; then
  open TestResults/CoverageReport/index.html
else
  echo "Please open TestResults/CoverageReport/index.html manually"
fi