#!/bin/bash

echo "Verifying build status..."

# Change to the project directory
cd "$(dirname "$0")"

# Build the entire solution
echo "Building solution..."
dotnet build --no-restore --verbosity minimal

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    
    # Run a quick test to verify tests compile
    echo "Running a quick test compilation check..."
    dotnet test --no-build --verbosity minimal --collect:"XPlat Code Coverage" --logger:"console;verbosity=minimal"
    
    if [ $? -eq 0 ]; then
        echo "✅ Tests compile and run successfully!"
        echo "✅ All issues have been resolved!"
    else
        echo "❌ Tests failed to run"
        echo "Check the output above for details"
    fi
else
    echo "❌ Build failed!"
    echo "Check the output above for compilation errors"
fi

echo "Verification complete."