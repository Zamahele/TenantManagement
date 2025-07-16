# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a comprehensive property management web application built with .NET 8 and ASP.NET Core MVC. The system manages rental properties, tenants, payments, maintenance requests, and operations with full observability stack.

## Architecture

**Clean Architecture with 4 layers:**
- **PropertyManagement.Domain**: Core business entities (Tenant, Room, Payment, LeaseAgreement, etc.)
- **PropertyManagement.Infrastructure**: Data access with EF Core, generic repository pattern, SQL Server
- **PropertyManagement.Application**: Application services (currently minimal)
- **PropertyManagement.Web**: MVC web application with Razor views, controllers, and ViewModels
- **PropertyManagement.Test**: xUnit tests with Moq

**Key Technologies:**
- .NET 8 with C# 12
- Entity Framework Core with SQL Server
- AutoMapper for object mapping
- FluentValidation for validation
- Serilog for structured logging
- Cookie-based authentication
- Bootstrap 5 + jQuery for UI
- Docker containerization

## Common Development Commands

**Build and Run:**
```bash
# Build entire solution
dotnet build

# Run web application
dotnet run --project PropertyManagement.Web

# Run with Docker Compose (full stack)
docker-compose up -d
```

**Testing:**
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test PropertyManagement.Test

# Run specific test class
dotnet test --filter "FullyQualifiedName~TenantsControllerTests"

# Run tests with code coverage
dotnet test PropertyManagement.Test --collect:"XPlat Code Coverage"

# Run tests with detailed output
dotnet test --verbosity detailed
```

**Database:**
```bash
# Add migration
dotnet ef migrations add MigrationName --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web

# Update database
dotnet ef database update --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web

# Drop database
dotnet ef database drop --project PropertyManagement.Infrastructure --startup-project PropertyManagement.Web
```

## Key Architectural Patterns

**Generic Repository Pattern:**
- `IGenericRepository<T>` and `GenericRepository<T>` in Infrastructure layer
- Provides standard CRUD operations with async support
- Includes filtering and eager loading via Expression trees

**Authentication & Authorization:**
- Cookie-based authentication with fallback policy requiring authentication globally
- Role-based access (Manager, Tenant roles)
- Uses BCrypt for password hashing

**Data Relationships:**
- User ↔ Tenant (1:1)
- Room ↔ Tenant (1:many)
- Tenant ↔ LeaseAgreement (1:many)
- Tenant ↔ Payment (1:many)
- Room ↔ LeaseAgreement (1:many)

**Validation:**
- FluentValidation with separate validator classes for each entity
- Client-side validation adapters enabled
- Validators registered in DI container

## File Upload & Storage

- File uploads stored in `wwwroot/uploads/` directory
- Proof of payment files stored in `wwwroot/uploads/proofs/`
- Lease agreement files have `FilePath` property on `LeaseAgreement` entity

## Observability Stack

**Logging:**
- Serilog with structured logging
- Multiple sinks: Console, File (`/app/logs/`), Elasticsearch
- JSON structured logs for production

**Monitoring:**
- Prometheus metrics exposed at `/metrics`
- Grafana dashboards
- OpenTelemetry collector for distributed tracing

**Infrastructure:**
- All services containerized with Docker Compose
- ELK stack (Elasticsearch, Kibana) for log aggregation
- SQL Server 2022 in container

## Default Credentials

- Manager login: Username: `Admin`, Password: `01Pa$$w0rd2025#`
- Database SA password: `Your_password123`

## Important Notes

- Database seeding controlled by `EnableDatabaseSeeding` configuration
- South African locale (en-ZA) configured by default
- HTTPS certificates located in `https/` directory
- Environment-specific settings in `appsettings.{Environment}.json`
- Web application serves both HTTP (80) and HTTPS (443) in production