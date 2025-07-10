# Use .NET 8 ASP.NET runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Add custom CA certificate if needed
COPY ca.crt /usr/local/share/ca-certificates/ca.crt
RUN update-ca-certificates

COPY ["PropertyManagement.Web/PropertyManagement.Web.csproj", "PropertyManagement.Web/"]
COPY ["PropertyManagement.Application/PropertyManagement.Application.csproj", "PropertyManagement.Application/"]
COPY ["PropertyManagement.Domain/PropertyManagement.Domain.csproj", "PropertyManagement.Domain/"]
COPY ["PropertyManagement.Infrastructure/PropertyManagement.Infrastructure.csproj", "PropertyManagement.Infrastructure/"]
RUN dotnet restore "./PropertyManagement.Web/PropertyManagement.Web.csproj"
COPY . .
WORKDIR "/src/PropertyManagement.Web"
RUN dotnet build "./PropertyManagement.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PropertyManagement.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Copy certificate for HTTPS
COPY ./aspnetapp.pfx /https/aspnetapp.pfx
ENTRYPOINT ["dotnet", "PropertyManagement.Web.dll"]