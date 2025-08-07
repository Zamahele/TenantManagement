# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Quick Start

For immediate productivity:
```bash
# 1. Build and test everything
dotnet build --configuration Release && ./run-tests.sh

# 2. Run application locally
cd PropertyManagement.Web && dotnet run

# 3. Apply/update database
dotnet ef database update

# 4. Access application: http://localhost:5000 (Admin/01Pa$$w0rd2025#)
```

## Essential Development Commands

### Build and Test
```bash
# Build entire solution
dotnet build --configuration Release

# Run all tests with coverage
./run-tests.sh

# Quick build verification
./verify-build.sh

# Run tests (PowerShell alternative)
./run-tests.ps1

# Run single test class
dotnet test --filter "ClassName=TenantControllerTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~GetTenantById_ReturnsCorrectViewModel"
```

### Database Operations
```bash
# Apply migrations
cd PropertyManagement.Web
dotnet ef database update

# Create new migration
dotnet ef migrations add MigrationName

# Reset database (drop and recreate)
dotnet ef database drop --force
dotnet ef database update
```

### Running the Application
```bash
# Development mode
cd PropertyManagement.Web
dotnet run

# Production with Docker Compose
docker-compose up -d

# Kubernetes deployment with Docker
kubectl apply -f k8s/

# Kubernetes deployment with Podman (Recommended)
./podman-k8s-deploy.sh
```

## High-Level Architecture

### Clean Architecture Structure
The codebase follows Clean Architecture with clear layer separation:

1. **PropertyManagement.Domain**: Core business entities without dependencies
2. **PropertyManagement.Application**: Business logic, DTOs, and service interfaces
3. **PropertyManagement.Infrastructure**: Data access, repositories, and external services
4. **PropertyManagement.Web**: Razor Pages presentation layer with ViewModels
5. **PropertyManagement.Test**: Comprehensive test suite

### Key Design Patterns
- **Repository Pattern**: Generic repository with Entity Framework
- **Service Layer Pattern**: Application services return `ServiceResult<T>` for consistent error handling
- **CQRS-style**: Separate DTOs for Create/Update operations (CreateTenantDto, UpdateTenantDto)
- **AutoMapper Configuration**: Extensive mapping between Entities ↔ DTOs ↔ ViewModels

### Technology Stack
- **.NET 8** with C# 12 features
- **ASP.NET Core Razor Pages** (NOT MVC or Blazor)
- **Entity Framework Core** with SQL Server
- **FluentValidation** for comprehensive input validation
- **AutoMapper** for object mapping across layers
- **PuppeteerSharp** for professional PDF generation
- **Prometheus** metrics with `/metrics` endpoint
- **Serilog** structured logging to files and console

## Critical AutoMapper Configuration

The application has extensive AutoMapper mappings that are essential for proper functioning:

### Entity ↔ DTO ↔ ViewModel Chain
```
Payment Entity ↔ PaymentDto ↔ PaymentViewModel
    ↓ requires
LeaseAgreementDto ↔ LeaseAgreementViewModel
    ↓ requires  
TenantDto ↔ TenantViewModel
    ↓ requires
UserDto ↔ UserViewModel
```

**CRITICAL**: When modifying AutoMapper configurations, ensure all nested object mappings exist. Missing mappings cause `AutoMapperMappingException` at runtime.

### Common AutoMapper Patterns
```csharp
// Controllers: DTOs from services → ViewModels for views
var result = await _tenantApplicationService.GetAllTenantsAsync();
var tenantVMs = _mapper.Map<List<TenantViewModel>>(result.Data);

// Services: Entities → DTOs for controllers
var tenant = await _tenantRepository.GetByIdAsync(id);
return ServiceResult<TenantDto>.Success(_mapper.Map<TenantDto>(tenant));
```

## Service Layer Pattern

All application services follow consistent patterns:

```csharp
public async Task<ServiceResult<TenantDto>> GetTenantByIdAsync(int id)
{
    var tenant = await _repository.GetByIdAsync(id);
    if (tenant == null)
        return ServiceResult<TenantDto>.Failure("Tenant not found");
    
    var dto = _mapper.Map<TenantDto>(tenant);
    return ServiceResult<TenantDto>.Success(dto);
}
```

Controllers handle ServiceResult consistently:
```csharp
var result = await _tenantApplicationService.GetTenantByIdAsync(id);
if (!result.IsSuccess) {
    SetErrorMessage(result.ErrorMessage);
    return View(new TenantViewModel());
}
```

## Authentication & Authorization

### Role-Based System
- **Manager**: Full system access, tenant management, reporting
- **Tenant**: Limited to own profile, payment history, maintenance requests

### Authentication Flow
- Cookie-based authentication via `TenantsController.Login()`
- Claims-based principal with UserId and Role claims
- Automatic role-based redirects after login
- Global authentication requirement with `[AllowAnonymous]` exceptions

### Default Credentials
- Username: `Admin`
- Password: `01Pa$$w0rd2025#`

## Database Schema & Relationships

### Core Entities
- **Tenant** → **Room** (one-to-one active occupancy)
- **Tenant** → **User** (one-to-one authentication)
- **Tenant** → **Payments** (one-to-many)
- **Tenant** → **LeaseAgreements** (one-to-many)
- **LeaseAgreement** → **DigitalSignature** (one-to-one)
- **LeaseTemplate** → **LeaseAgreements** (one-to-many templates)
- **Room** → **MaintenanceRequests** (one-to-many)
- **Room** → **Inspections** (one-to-many)
- **Room** → **BookingRequests** (one-to-many)

### Key Business Rules
- Unique room numbers and tenant-room assignments
- South African phone number validation (0821234567)
- Date validations preventing future dates for historical records
- Room status constraints (Available, Occupied, Under Maintenance)

## UI Architecture & Components

### Razor Pages Structure
Controllers are **NOT MVC controllers** - they're Razor Pages controllers:
- **HomeController**: Dashboard with property statistics
- **TenantsController**: Complete tenant lifecycle management
- **RoomsController**: Room management and booking system
- **PaymentsController**: Payment tracking with receipt generation
- **MaintenanceController**: Maintenance workflow
- **InspectionsController**: Room inspection scheduling
- **LeaseAgreementsController**: Digital lease management
- **DigitalLeaseController**: Digital lease generation and signing workflow
- **UtilityBillsController**: Utility billing system

### Advanced Table Pagination System
The application features a comprehensive client-side pagination system:

```html
<!-- Basic implementation -->
<table id="myTable" data-pagination data-items-per-page="10">
    <!-- table content -->
</table>

<!-- With search functionality -->
@{
    ViewData["SearchId"] = "search-input";
    ViewData["SearchPlaceholder"] = "Search payments...";
}
@await Html.PartialAsync("_TableSearch")

<table id="paymentsTable" 
       data-pagination 
       data-search-input="#search-input"
       data-items-per-page="15">
    <!-- table content -->
</table>
```

### Modal-Based CRUD Operations
- Add/Edit operations use Bootstrap modals loaded via AJAX
- Partial views (`_TenantForm.cshtml`, `_PaymentModals.cshtml`) return form content
- JavaScript handles modal lifecycle and form submission
- Consistent `BaseController` provides `SetSuccessMessage()`, `SetErrorMessage()`, `SetInfoMessage()`

## FluentValidation System

### Validation Architecture
Each entity has corresponding validator classes with comprehensive rules:
- **Client-side**: jQuery validation for immediate feedback
- **Server-side**: FluentValidation in controller actions  
- **Database**: Entity constraints and referential integrity

### Key Validation Rules
```csharp
// South African phone numbers
RuleFor(t => t.CellphoneNumber)
    .Matches(@"^0[6-8][0-9]{8}$")
    .WithMessage("Cellphone number must be in South African format (e.g., 0821234567)");

// Unique constraints
RuleForEach(room => room.Tenants)
    .Must(tenant => /* unique validation logic */)
    .WithMessage("Room can only have one active tenant");
```

## Digital Lease Management System

### Advanced Digital Lease Features
The application includes a comprehensive digital lease system with professional lease generation and electronic signing capabilities:

#### Key Components
- **LeaseGenerationService**: Professional PDF/HTML lease document generation using PuppeteerSharp
- **DigitalSignature**: Electronic signature capture and storage
- **LeaseTemplate**: Template-based lease generation with customizable terms
- **Workflow Management**: Complete lease lifecycle from draft to signed execution

#### Digital Lease Workflow
1. **Lease Generation**: Manager generates professional lease documents with tenant/room data
2. **Document Preview**: HTML preview with professional formatting and styling
3. **PDF Creation**: Automated PDF generation with embedded signatures
4. **Digital Signing**: Canvas-based signature capture with timestamp validation
5. **Document Storage**: Secure file storage with access controls
6. **Status Tracking**: Complete audit trail of lease status changes

#### Technical Implementation
```csharp
// Professional lease generation with PuppeteerSharp
var pdfResult = await _leaseGenerationService.GenerateLeasePdfAsync(leaseId, htmlContent);

// Digital signature processing
var signatureResult = await _leaseGenerationService.ProcessDigitalSignatureAsync(
    leaseId, signatureData, ipAddress, userAgent);
```

### File Upload & Document Management

#### Supported Operations
- **Lease Agreements**: PDF upload with file validation
- **Digital Leases**: Professional PDF generation with embedded signatures
- **Payment Proofs**: PDF/Image receipt uploads
- **Receipt Generation**: Automated PDF receipt creation
- **Signature Storage**: PNG signature files with metadata
- **File Security**: Restricted file types and size limits

#### Upload Locations
- `/wwwroot/uploads/leases/` - Generated lease documents (PDF/HTML)
- `/wwwroot/uploads/signatures/` - Digital signature images
- `/wwwroot/uploads/` - Lease agreements
- `/wwwroot/uploads/proofs/` - Payment proofs

## Monitoring & Observability

### Prometheus Integration
- Custom business metrics (payment creation counters)
- Default HTTP metrics via `app.UseHttpMetrics()`
- Metrics endpoint available at `/metrics`

### Logging Strategy
- **Serilog** with structured logging
- File logging to `/app/logs/` (production) 
- Console logging for development
- Elasticsearch integration for log aggregation

## Docker & Kubernetes Deployment

### Local Development with Docker Compose
```bash
# Start full stack with monitoring
docker-compose up -d

# Access services:
# App: http://localhost:8080
# Prometheus: http://localhost:9090  
# Grafana: http://localhost:3000
# Kibana: http://localhost:5601
```

### Kubernetes Production with Podman (Recommended)
```bash
# One-command deployment with Podman
./podman-k8s-deploy.sh

# Manual steps if needed:
# 1. Build image with Podman
podman build -t property-management:latest -f PropertyManagement.Web/Dockerfile .

# 2. Create HTTPS certificate secret
kubectl create secret generic https-cert --from-file=aspnetapp.pfx=aspnetapp.pfx -n property-management

# 3. Deploy to Kubernetes
kubectl apply -f kubernetes/

# 4. Monitor deployment
kubectl get all -n property-management

# 5. Access application (HTTPS recommended)
kubectl port-forward svc/property-management-service 8443:443 -n property-management
# Then access: https://localhost:8443

# Alternative HTTP access
kubectl port-forward svc/property-management-service 8080:80 -n property-management
# Then access: http://localhost:8080
```

### Kubernetes Production with Docker
```bash
# Build image with Docker
docker build -t your-registry/property-management:latest -f PropertyManagement.Web/Dockerfile .

# Push to registry (if using remote cluster)
docker push your-registry/property-management:latest

# Deploy to Kubernetes
kubectl apply -f k8s/

# Monitor deployment
kubectl get all -n property-management

# Check logs
kubectl logs -f deployment/property-management-web -n property-management
```

### Podman-Specific Benefits
- **Rootless containers**: Enhanced security with rootless execution
- **Daemonless**: No background daemon required
- **Pod support**: Native Kubernetes pod simulation
- **OCI compatibility**: Full Docker compatibility
- **Local development**: Better integration with local Kubernetes (kind, k3s)

## Testing Strategy

### Test Structure
```
PropertyManagement.Test/
├── Controllers/     # Controller action testing with mocked services
├── Domain/         # Entity and business logic tests
├── Infrastructure/ # Repository and database tests
└── ViewModels/     # ViewModel validation tests
```

### Testing Patterns
```csharp
// Arrange: Setup test data and mocks
var mockService = new Mock<ITenantApplicationService>();
var mapper = GetMapper(); // From TestBaseClass
var controller = new TenantsController(mockService.Object, mapper);

// Act: Execute the action
var result = await controller.Index();

// Assert: Verify expected behavior
var viewResult = Assert.IsType<ViewResult>(result);
Assert.IsAssignableFrom<IEnumerable<TenantViewModel>>(viewResult.Model);
```

### Running Tests
- `./run-tests.sh` - Full test suite with coverage
- `./verify-build.sh` - Quick build verification
- Tests use in-memory database for isolation

## Common Development Scenarios

### Adding New Entities
1. Create entity in `PropertyManagement.Domain/Entities/`
2. Add corresponding DTO in `PropertyManagement.Application/DTOs/`
3. Create ViewModel in `PropertyManagement.Web/ViewModels/`
4. Configure AutoMapper mappings in `Program.cs`
5. Add FluentValidation validator
6. Create migration: `dotnet ef migrations add AddEntityName`

### Debugging AutoMapper Issues
Search for these patterns when troubleshooting:
- `AutoMapperMappingException` - Missing nested mappings
- `CreateMap`, `ForMember`, `MapFrom`, `Ignore` - Configuration syntax
- DTO class names ending in `Dto`
- ViewModel class names ending in `ViewModel`

### Performance Optimization
- Use `IQueryable` projections for large datasets
- Implement table pagination for data-heavy views
- Monitor `/metrics` endpoint for performance insights
- Use Serilog structured logging for performance tracking

## Security Considerations

### Input Security
- FluentValidation prevents malicious input
- Entity Framework parameterized queries prevent SQL injection
- File upload restrictions (type, size, location)
- Authentication required globally except `[AllowAnonymous]` actions

### Data Protection
- Password hashing with BCrypt
- Sensitive data excluded from DTOs (PasswordHash not in UserDto)
- Role-based data access (tenants see only own data)
- HTTPS configuration for production

## Recent Critical Fixes & Enhancements

### Digital Lease Management System (August 2025)
- ✅ **NEW FEATURE**: Complete digital lease generation and signing workflow
- ✅ **PDF Generation**: Professional lease documents using PuppeteerSharp
- ✅ **Digital Signatures**: Canvas-based signature capture with metadata storage
- ✅ **Lease Templates**: Template-based document generation system
- ✅ **Security**: IP address and user agent tracking for signature validation
- ✅ **File Management**: Organized storage structure for leases and signatures
- ✅ **Status Workflow**: Complete lease lifecycle tracking (Draft → Pending → Signed)

### AutoMapper Resolution (December 2024)
- ✅ Fixed `UserDto → UserViewModel` mapping exception
- ✅ Fixed `LeaseAgreementDto → LeaseAgreementViewModel` mapping exception  
- ✅ Fixed `PaymentDto → PaymentViewModel` mapping with nested objects
- ✅ Added comprehensive nested object mapping support
- ✅ Updated test configurations to match main application mappings

### Add Tenant Functionality Fix (August 2024)
- ✅ **CRITICAL FIX**: Fixed missing `ViewBag.Rooms` population in `TenantsController.TenantForm()` action
- ✅ Added `IRoomApplicationService` dependency injection to TenantsController
- ✅ Updated TenantForm GET action to load available rooms for dropdown selection
- ✅ Resolved "Add Tenant not working" issue that prevented room selection during tenant creation

### Comprehensive Test Suite Enhancement (August 2024)
- ✅ Created complete **Service Layer Tests** (TenantApplicationServiceTests, PaymentApplicationServiceTests, RoomApplicationServiceTests, MaintenanceApplicationServiceTests)
- ✅ Added **Integration Tests** (TenantIntegrationTests, PaymentIntegrationTests) with real database operations
- ✅ Enhanced **Domain Entity Tests** with comprehensive business logic validation
- ✅ Implemented **Cross-Service Integration Testing** for complex workflows
- ✅ Added **Performance and Concurrency Tests** for bulk operations
- ✅ Created **Data Consistency Tests** for transaction integrity

**Test Coverage Summary**:
- **Controller Layer**: 95% ✅ (Comprehensive CRUD coverage)
- **Service Layer**: 85% ✅ (New comprehensive tests added)
- **Repository Layer**: 80% ✅ (Generic repository fully tested)
- **Domain Layer**: 90% ✅ (Enhanced business logic tests)
- **Integration**: 75% ✅ (New integration tests added)
- **Overall Coverage**: 85% ✅ (Previously 70%)

**Note**: When working with AutoMapper, always ensure nested object mappings exist for complex object hierarchies.

## Troubleshooting Common Issues

### AutoMapper Exceptions
```bash
# Search for AutoMapper configurations
rg "CreateMap|ForMember" --type cs

# Find specific DTO/ViewModel mappings
rg "Map<.*ViewModel>" --type cs -A 2 -B 2
```

**Common fixes:**
- Missing nested object mappings (add `CreateMap<NestedDto, NestedViewModel>()`)
- Circular reference (use `MaxDepth()` configuration)
- Property name mismatches (use `ForMember().MapFrom()`)

### Build/Test Failures
```bash
# Clean and rebuild
dotnet clean && dotnet build --configuration Release

# Reset database if migrations fail
dotnet ef database drop --force && dotnet ef database update

# Check for missing NuGet packages
dotnet restore --force
```

### Performance Issues
```bash
# Monitor metrics during development
curl http://localhost:5000/metrics

# Check database query performance
# Enable EF logging in appsettings.Development.json:
# "Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

## Command Line Development Shortcuts

### Build and Development Shortcuts
- **Build command**: `dotnet build` with specific configuration
- **Export PATH for dotnet**: `export PATH="$PATH:$HOME/.dotnet"`
- **Build in TenantManagement repo**: `Bash(export PATH="$PATH:$HOME/.dotnet" && cd /mnt/c/Users/zamajasonn/source/repos/TenantManagement && dotnet build)`