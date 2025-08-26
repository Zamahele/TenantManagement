# Use .NET 8 ASP.NET runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Configure DNS and networking for better NuGet connectivity
RUN apt-get update && apt-get install -y curl dnsutils iputils-ping

# Set DNS servers explicitly
# RUN echo 'nameserver 8.8.8.8' > /etc/resolv.conf && \
#     echo 'nameserver 8.8.4.4' >> /etc/resolv.conf

# Test connectivity to NuGet
RUN curl -I https://api.nuget.org/v3/index.json || echo "NuGet connectivity test failed, continuing..."

# Configure NuGet to use HTTP fallback if needed
RUN dotnet nuget list source
RUN dotnet nuget add source https://api.nuget.org/v3/index.json --name "nuget.org" --protocol-version 3 || echo "NuGet source already exists"

# Copy project files for restore
COPY ["PropertyManagement.Web/PropertyManagement.Web.csproj", "PropertyManagement.Web/"]
COPY ["PropertyManagement.Application/PropertyManagement.Application.csproj", "PropertyManagement.Application/"]
COPY ["PropertyManagement.Domain/PropertyManagement.Domain.csproj", "PropertyManagement.Domain/"]
COPY ["PropertyManagement.Infrastructure/PropertyManagement.Infrastructure.csproj", "PropertyManagement.Infrastructure/"]

# Restore with retries and better error handling
RUN for i in 1 2 3; do \
        echo "Restore attempt $i..."; \
        dotnet restore "./PropertyManagement.Web/PropertyManagement.Web.csproj" \
            --disable-parallel \
            --verbosity normal \
            --runtime linux-x64 \
            --no-cache \
            && break || \
        (echo "Restore attempt $i failed, retrying..." && sleep 10); \
    done

# Copy all source code
COPY . .
WORKDIR "/src/PropertyManagement.Web"

# Build the application
RUN dotnet build "./PropertyManagement.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PropertyManagement.Web.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create https directory if it doesn't exist
RUN mkdir -p /https

# Copy certificate for HTTPS (make it optional)
# COPY PropertyManagement.Web/https/aspnetapp.pfx /https/aspnetapp.pfx || echo "No HTTPS certificate found, skipping..."

# Verify the published files
RUN ls -la /app

ENTRYPOINT ["dotnet", "PropertyManagement.Web.dll"]