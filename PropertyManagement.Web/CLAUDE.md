# CLAUDE.md - AI Assistant Context & Search Guide

## Project Overview
**Property Management System** - A comprehensive ASP.NET Core 8 Razor Pages application for managing rental properties, tenants, payments, maintenance, and operations.

### Technology Stack
- **.NET 8** with C# 12
- **ASP.NET Core Razor Pages** (NOT Blazor or MVC)
- **Entity Framework Core** with SQL Server
- **FluentValidation** for business rules
- **AutoMapper** for object mapping
- **xUnit & Moq** for testing
- **Prometheus** for metrics
- **Bootstrap 5** for UI

## Kubernetes Deployment

### Overview
The Property Management System can be deployed to Kubernetes using the comprehensive manifest files and configuration provided below. The deployment includes SQL Server database, web application, persistent storage, and monitoring capabilities.

### Quick Kubernetes Deployment Guide

#### 1. Build and Push Docker Image
```bash
# Build the Docker image (from solution root)
docker build -t your-registry/property-management:latest -f PropertyManagement.Web/Dockerfile .

# Push to your container registry
docker push your-registry/property-management:latest
```

#### 2. Create Kubernetes Manifest Files

Create the following YAML files in a `k8s/` directory:

**namespace.yaml**
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: property-management
  labels:
    app: property-management
    environment: production
```

**secrets.yaml**
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sql-server-secret
  namespace: property-management
type: Opaque
data:
  # Base64 encoded: Your_password123
  SA_PASSWORD: WW91cl9wYXNzd29yZDEyMw==
---
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
  namespace: property-management
type: Opaque
data:
  # Base64 encoded: Production
  ASPNETCORE_ENVIRONMENT: UHJvZHVjdGlvbg==
```

**configmap.yaml**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
  namespace: property-management
data:
  appsettings.Production.json: |
    {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning"
            }
        },
        "EnableDatabaseSeeding": true,
        "ConnectionStrings": {
            "DefaultConnection": "Server=sql-server-service,1433;Database=PropertyManagementDb;User=sa;Password=Your_password123;MultipleActiveResultSets=true;TrustServerCertificate=True"
        },
        "AllowedHosts": "*",
        "UtilityRates": {
            "WaterPerLiter": 0.02,
            "ElectricityPerKwh": 1.50
        },
        "Serilog": {
            "MinimumLevel": "Information",
            "WriteTo": [
                {
                    "Name": "File",
                    "Args": {
                        "path": "/app/logs/propertymanagement.log",
                        "rollingInterval": "Day"
                    }
                },
                {
                    "Name": "Console"
                }
            ]
        }
    }
```

**storage.yaml**
```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: sql-server-pvc
  namespace: property-management
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 20Gi
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: app-logs-pvc
  namespace: property-management
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 5Gi
```

**sql-server.yaml**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sql-server
  namespace: property-management
spec:
  replicas: 1
  selector:
    matchLabels:
      app: sql-server
  template:
    metadata:
      labels:
        app: sql-server
    spec:
      containers:
      - name: sql-server
        image: mcr.microsoft.com/mssql/server:2022-latest
        ports:
        - containerPort: 1433
        env:
        - name: ACCEPT_EULA
          value: "Y"
        - name: SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: sql-server-secret
              key: SA_PASSWORD
        - name: MSSQL_PID
          value: "Express"
        volumeMounts:
        - name: sql-storage
          mountPath: /var/opt/mssql
        resources:
          requests:
            memory: "2Gi"
            cpu: "500m"
          limits:
            memory: "4Gi"
            cpu: "1"
        livenessProbe:
          exec:
            command:
            - /opt/mssql-tools/bin/sqlcmd
            - -S
            - localhost
            - -U
            - sa
            - -P
            - Your_password123
            - -Q
            - SELECT 1
          initialDelaySeconds: 60
          periodSeconds: 30
        readinessProbe:
          exec:
            command:
            - /opt/mssql-tools/bin/sqlcmd
            - -S
            - localhost
            - -U
            - sa
            - -P
            - Your_password123
            - -Q
            - SELECT 1
          initialDelaySeconds: 30
          periodSeconds: 10
      volumes:
      - name: sql-storage
        persistentVolumeClaim:
          claimName: sql-server-pvc
```

**web-app.yaml**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: property-management-web
  namespace: property-management
spec:
  replicas: 2
  selector:
    matchLabels:
      app: property-management-web
  template:
    metadata:
      labels:
        app: property-management-web
    spec:
      initContainers:
      - name: wait-for-db
        image: busybox:1.35
        command: 
        - sh
        - -c
        - |
          until nc -z sql-server-service 1433; do
            echo "Waiting for SQL Server..."
            sleep 5
          done
      containers:
      - name: web-app
        image: your-registry/property-management:latest  # UPDATE THIS
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          value: "Server=sql-server-service,1433;Database=PropertyManagementDb;User=sa;Password=Your_password123;MultipleActiveResultSets=true;TrustServerCertificate=True"
        volumeMounts:
        - name: app-config
          mountPath: /app/appsettings.Production.json
          subPath: appsettings.Production.json
        - name: app-logs
          mountPath: /app/logs
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /
            port: 80
          initialDelaySeconds: 60
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
      volumes:
      - name: app-config
        configMap:
          name: app-config
      - name: app-logs
        persistentVolumeClaim:
          claimName: app-logs-pvc
```

**services.yaml**
```yaml
apiVersion: v1
kind: Service
metadata:
  name: sql-server-service
  namespace: property-management
spec:
  selector:
    app: sql-server
  ports:
  - port: 1433
    targetPort: 1433
  type: ClusterIP
---
apiVersion: v1
kind: Service
metadata:
  name: property-management-service
  namespace: property-management
spec:
  selector:
    app: property-management-web
  ports:
  - port: 80
    targetPort: 80
  type: LoadBalancer
```

**ingress.yaml** (Optional)
```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: property-management-ingress
  namespace: property-management
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  rules:
  - host: property-management.your-domain.com  # UPDATE THIS
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: property-management-service
            port:
              number: 80
```

#### 3. Deploy to Kubernetes
```bash
# Create k8s directory and save the YAML files above
mkdir k8s
cd k8s

# Deploy in order
kubectl apply -f namespace.yaml
kubectl apply -f secrets.yaml
kubectl apply -f configmap.yaml
kubectl apply -f storage.yaml
kubectl apply -f sql-server.yaml

# Wait for SQL Server to be ready
kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n property-management

# Deploy web application
kubectl apply -f web-app.yaml
kubectl apply -f services.yaml
kubectl apply -f ingress.yaml  # Optional
```

#### 4. Verify and Access
```bash
# Check deployment status
kubectl get all -n property-management

# Get external IP (LoadBalancer)
kubectl get svc property-management-service -n property-management

# Check logs
kubectl logs -f deployment/property-management-web -n property-management

# Port forward for local testing
kubectl port-forward svc/property-management-service 8080:80 -n property-management
```

### Kubernetes Deployment Features

- **High Availability**: 2 replicas of web application with health checks
- **Persistent Storage**: SQL Server data and application logs are persisted
- **Auto-scaling Ready**: Can be easily configured with HPA (Horizontal Pod Autoscaler)
- **Monitoring**: Prometheus metrics endpoint available at `/metrics`
- **Security**: Secrets management for sensitive data
- **Health Checks**: Liveness and readiness probes for reliability

### Production Considerations

1. **Update Image Reference**: Replace `your-registry/property-management:latest` with your actual image
2. **Security**: Change default passwords in secrets for production
3. **SSL/TLS**: Configure HTTPS and proper certificates
4. **Resource Scaling**: Adjust CPU/memory limits based on load
5. **Storage Classes**: Configure appropriate storage classes for your cluster
6. **Backup Strategy**: Implement database backup for SQL Server persistent data
7. **Monitoring**: Add Prometheus/Grafana for comprehensive monitoring

### Scaling Operations
```bash
# Scale web application
kubectl scale deployment property-management-web --replicas=5 -n property-management

# Enable horizontal pod autoscaler
kubectl autoscale deployment property-management-web --cpu-percent=50 --min=2 --max=10 -n property-management
```

### Default Login (Post-Deployment)
- **Username**: `Admin`
- **Password**: `01Pa$$w0rd2025#`

## Architecture & Project Structure

### Solution Projects
```
PropertyManagement.Domain/          # Core entities and domain logic
PropertyManagement.Application/     # Business logic and DTOs
PropertyManagement.Infrastructure/  # Data access and repositories
PropertyManagement.Web/            # Razor Pages UI layer
PropertyManagement.Test/           # Unit tests
```

### Clean Architecture Layers
1. **Domain Layer**: Core business entities and rules
2. **Application Layer**: Use cases, services, DTOs, business logic
3. **Infrastructure Layer**: Database context, repositories, external services
4. **Presentation Layer**: Razor Pages, controllers, view models

## AutoMapper Configuration

### Main Application Configuration (Program.cs)
The application uses AutoMapper for mapping between different object types across layers. Here's the complete configuration:

#### Entity to ViewModel Mappings
```csharp
// Basic entity to ViewModel mappings
cfg.CreateMap<Room, RoomViewModel>().ReverseMap();
cfg.CreateMap<User, UserViewModel>().ReverseMap();
cfg.CreateMap<Payment, PaymentViewModel>();
cfg.CreateMap<PaymentViewModel, Payment>()
    .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => src.PaymentId ?? 0))
    .ForMember(dest => dest.Date, opt => opt.Ignore()); // Set in controller

cfg.CreateMap<RoomFormViewModel, Room>().ReverseMap();
cfg.CreateMap<LeaseAgreement, LeaseAgreementViewModel>()
    .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
    .ReverseMap();

// Complex entity mappings with navigation properties
cfg.CreateMap<Tenant, TenantViewModel>()
    .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
    .ForMember(dest => dest.LeaseAgreements, opt => opt.MapFrom(src => src.LeaseAgreements))
    .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments))
    .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));
cfg.CreateMap<TenantViewModel, Tenant>()
    .ForMember(dest => dest.Room, opt => opt.Ignore())
    .ForMember(dest => dest.LeaseAgreements, opt => opt.Ignore())
    .ForMember(dest => dest.Payments, opt => opt.Ignore())
    .ForMember(dest => dest.User, opt => opt.Ignore());

cfg.CreateMap<BookingRequest, BookingRequestViewModel>()
    .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
    .ForMember(dest => dest.RoomOptions, opt => opt.Ignore());
cfg.CreateMap<BookingRequestViewModel, BookingRequest>()
    .ForMember(dest => dest.Room, opt => opt.Ignore());

cfg.CreateMap<Inspection, InspectionViewModel>().ReverseMap();
cfg.CreateMap<InspectionViewModel, Inspection>()
    .ForMember(dest => dest.Room, opt => opt.Ignore());

cfg.CreateMap<MaintenanceRequest, MaintenanceRequestViewModel>().ReverseMap();
cfg.CreateMap<MaintenanceRequestViewModel, MaintenanceRequest>()
    .ForMember(dest => dest.Room, opt => opt.Ignore());
```

#### DTO to ViewModel Mappings (Critical for Application Services)
```csharp
// Core DTO to ViewModel mappings
cfg.CreateMap<TenantDto, TenantViewModel>()
    .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
    .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

// **CRITICAL MAPPING** - UserDto to UserViewModel
cfg.CreateMap<UserDto, UserViewModel>()
    .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // PasswordHash not in UserDto for security
    .ReverseMap()
    .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
    .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
    .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

// **CRITICAL MAPPING** - LeaseAgreementDto to LeaseAgreementViewModel
cfg.CreateMap<LeaseAgreementDto, LeaseAgreementViewModel>()
    .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
    .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
    .ForMember(dest => dest.File, opt => opt.Ignore()) // IFormFile is not in DTO
    .ForMember(dest => dest.RentDueDate, opt => opt.Ignore()); // Computed property

// **CRITICAL MAPPING** - PaymentDto to PaymentViewModel with nested objects
cfg.CreateMap<PaymentDto, PaymentViewModel>()
    .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.PaymentDate))
    .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
    .ForMember(dest => dest.LeaseAgreement, opt => opt.MapFrom(src => src.LeaseAgreement))
    .ForMember(dest => dest.Room, opt => opt.Ignore()); // Room comes through Tenant navigation

cfg.CreateMap<RoomDto, RoomViewModel>().ReverseMap();
cfg.CreateMap<RoomWithTenantsDto, RoomViewModel>();
cfg.CreateMap<LeaseAgreementDto, LeaseAgreementViewModel>().ReverseMap();
cfg.CreateMap<MaintenanceRequestDto, MaintenanceRequestViewModel>().ReverseMap();
cfg.CreateMap<BookingRequestDto, BookingRequestViewModel>().ReverseMap();
cfg.CreateMap<InspectionDto, InspectionViewModel>().ReverseMap();
cfg.CreateMap<UtilityBillDto, UtilityBillDto>().ReverseMap(); // Note: typo in original config
```

#### Entity to DTO Mappings
```csharp
cfg.CreateMap<Tenant, TenantDto>().ReverseMap();
cfg.CreateMap<User, UserDto>().ReverseMap();
cfg.CreateMap<Room, RoomDto>().ReverseMap();
cfg.CreateMap<Room, RoomWithTenantsDto>().ReverseMap();
cfg.CreateMap<Payment, PaymentDto>().ReverseMap();
cfg.CreateMap<LeaseAgreement, LeaseAgreementDto>().ReverseMap();
cfg.CreateMap<BookingRequest, BookingRequestDto>().ReverseMap();
cfg.CreateMap<MaintenanceRequest, MaintenanceRequestDto>().ReverseMap();
cfg.CreateMap<Inspection, InspectionDto>().ReverseMap();
cfg.CreateMap<UtilityBill, UtilityBillDto>().ReverseMap();
```

#### Create/Update DTO Mappings
```csharp
// Command DTOs for create operations
cfg.CreateMap<CreateRoomDto, Room>();
cfg.CreateMap<CreateBookingRequestDto, BookingRequest>();
cfg.CreateMap<CreateMaintenanceRequestDto, MaintenanceRequest>();
cfg.CreateMap<CreateInspectionDto, Inspection>();
cfg.CreateMap<CreateUtilityBillDto, UtilityBill>();

// Command DTOs for update operations
cfg.CreateMap<UpdateRoomDto, Room>();
cfg.CreateMap<UpdateBookingRequestDto, BookingRequest>();
cfg.CreateMap<UpdateMaintenanceRequestDto, MaintenanceRequest>();
cfg.CreateMap<UpdateInspectionDto, Inspection>();
cfg.CreateMap<UpdateUtilityBillDto, UtilityBill>();

// ViewModel to Create/Update DTO mappings
cfg.CreateMap<TenantViewModel, CreateTenantDto>();
cfg.CreateMap<TenantViewModel, UpdateTenantDto>();
cfg.CreateMap<TenantViewModel, RegisterTenantDto>();
cfg.CreateMap<PaymentViewModel, CreatePaymentDto>();
cfg.CreateMap<PaymentViewModel, UpdatePaymentDto>();
cfg.CreateMap<RoomFormViewModel, CreateRoomDto>();
cfg.CreateMap<RoomFormViewModel, UpdateRoomDto>();
cfg.CreateMap<BookingRequestViewModel, CreateBookingRequestDto>();
cfg.CreateMap<BookingRequestViewModel, UpdateBookingRequestDto>();
cfg.CreateMap<InspectionViewModel, CreateInspectionDto>();
cfg.CreateMap<InspectionViewModel, UpdateInspectionDto>();
cfg.CreateMap<MaintenanceRequestViewModel, CreateMaintenanceRequestDto>();
cfg.CreateMap<MaintenanceRequestViewModel, UpdateMaintenanceRequestDto>();
```

### Test Configuration Patterns
Test files use similar but simplified configurations:

#### Basic Test Configuration (TestBaseClass.cs)
```csharp
protected IMapper GetMapper()
{
    var config = new MapperConfiguration(cfg =>
    {
        // Entity to ViewModel mappings
        cfg.CreateMap<Room, RoomViewModel>().ReverseMap();
        cfg.CreateMap<Tenant, TenantViewModel>().ReverseMap();
        cfg.CreateMap<User, UserViewModel>().ReverseMap();
        
        // DTO to ViewModel mappings (comprehensive test coverage)
        cfg.CreateMap<TenantDto, TenantViewModel>()
            .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
            .ReverseMap();
            
        cfg.CreateMap<UserDto, UserViewModel>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ReverseMap();
            
        // CRITICAL: LeaseAgreement and Payment DTO mappings
        cfg.CreateMap<LeaseAgreementDto, LeaseAgreementViewModel>()
            .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
            .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
            .ForMember(dest => dest.File, opt => opt.Ignore())
            .ForMember(dest => dest.RentDueDate, opt => opt.Ignore())
            .ReverseMap();
            
        cfg.CreateMap<PaymentDto, PaymentViewModel>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.PaymentDate))
            .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
            .ForMember(dest => dest.LeaseAgreement, opt => opt.MapFrom(src => src.LeaseAgreement))
            .ForMember(dest => dest.Room, opt => opt.Ignore())
            .ReverseMap();
        
        // Entity to DTO mappings
        cfg.CreateMap<Tenant, TenantDto>().ReverseMap();
        cfg.CreateMap<User, UserDto>().ReverseMap();
        cfg.CreateMap<Room, RoomDto>().ReverseMap();
        cfg.CreateMap<Payment, PaymentDto>().ReverseMap();
        cfg.CreateMap<LeaseAgreement, LeaseAgreementDto>().ReverseMap();
    }, NullLoggerFactory.Instance);
    return config.CreateMapper();
}
```

### AutoMapper Usage Patterns

#### In Controllers
```csharp
// Application service returns DTOs, map to ViewModels for views
var result = await _tenantApplicationService.GetAllTenantsAsync();
var tenantVms = _mapper.Map<List<TenantViewModel>>(result.Data);
return View(tenantVms);

// Map ViewModels to command DTOs for service calls
var createTenantDto = _mapper.Map<CreateTenantDto>(tenantViewModel);
var result = await _tenantApplicationService.CreateTenantAsync(createTenantDto);
```

#### In Application Services
```csharp
// Map entities to DTOs for returning to controllers
var tenant = await _tenantRepository.GetByIdAsync(id);
var tenantDto = _mapper.Map<TenantDto>(tenant);
return ServiceResult<TenantDto>.Success(tenantDto);

// Map command DTOs to entities for persistence
var tenant = _mapper.Map<Tenant>(createTenantDto);
await _tenantRepository.AddAsync(tenant);
```

### Common AutoMapper Issues & Solutions

#### Missing Nested Object Mappings
**Problem**: `AutoMapperMappingException` when mapping objects with nested properties
**Solution**: Ensure all nested objects have explicit mappings (e.g., UserDto → UserViewModel, LeaseAgreementDto → LeaseAgreementViewModel)

**Example Fix**:
```csharp
// PaymentDto contains LeaseAgreementDto LeaseAgreement property
// PaymentViewModel contains LeaseAgreementViewModel LeaseAgreement property
// Need explicit mapping: LeaseAgreementDto → LeaseAgreementViewModel

cfg.CreateMap<LeaseAgreementDto, LeaseAgreementViewModel>()
    .ForMember(dest => dest.Tenant, opt => opt.MapFrom(src => src.Tenant))
    .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room));
```

#### Ignored Properties
**Problem**: Properties that shouldn't be mapped (navigation properties, computed fields)
**Solution**: Use `.ForMember(dest => dest.Property, opt => opt.Ignore())`

#### Property Name Differences
**Problem**: Different property names between source and destination
**Solution**: Use `.ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.PaymentDate))`

#### Security Considerations
**Problem**: Sensitive data in ViewModels/DTOs
**Solution**: Use `.ForMember()` to ignore sensitive properties like PasswordHash

### Critical Mapping Dependencies

#### Payment Mapping Chain
```
Payment (Entity) ↔ PaymentDto ↔ PaymentViewModel
                     ↓
         Requires: LeaseAgreementDto ↔ LeaseAgreementViewModel
                     ↓
         Requires: TenantDto ↔ TenantViewModel
                     ↓
         Requires: UserDto ↔ UserViewModel
```

#### Tenant Mapping Chain
```
Tenant (Entity) ↔ TenantDto ↔ TenantViewModel
                   ↓
       Requires: UserDto ↔ UserViewModel
                   ↓
       Requires: RoomDto ↔ RoomViewModel
```

### AutoMapper Profile Recommendations
For future scalability, consider organizing mappings into AutoMapper profiles:
```csharp
public class TenantMappingProfile : Profile
{
    public TenantMappingProfile()
    {
        CreateMap<Tenant, TenantViewModel>();
        CreateMap<TenantDto, TenantViewModel>();
        // ... other tenant-related mappings
    }
}
```

## Domain Entities

### Core Entities
- **Tenant**: Rental property occupants with contact info, emergency contacts
- **Room**: Individual rental units with status (Available, Occupied, Under Maintenance)
- **User**: Authentication system (Manager/Tenant roles)
- **Payment**: Rent and deposit tracking with receipts
- **LeaseAgreement**: Digital lease contracts with terms and dates
- **MaintenanceRequest**: Property maintenance tracking and workflow
- **Inspection**: Room inspection records and results
- **BookingRequest**: Prospective tenant room booking system
- **UtilityBill**: Water and electricity usage tracking

### Key Relationships
- Tenant → Room (one-to-one active occupancy)
- Tenant → User (one-to-one authentication)
- Tenant → Payments (one-to-many)
- Tenant → LeaseAgreements (one-to-many)
- Room → MaintenanceRequests (one-to-many)
- Room → Inspections (one-to-many)
- Room → BookingRequests (one-to-many)

## Controllers & Views Mapping

### Razor Pages Controllers (NOT MVC Controllers)
- **HomeController**: Dashboard with property statistics
- **TenantsController**: Tenant management (CRUD, auth, profile)
- **RoomsController**: Room management and booking system
- **PaymentsController**: Payment tracking and receipt generation
- **MaintenanceController**: Maintenance request workflow
- **InspectionsController**: Room inspection scheduling
- **LeaseAgreementsController**: Lease contract management
- **UtilityBillsController**: Utility billing system

### View Model Patterns
All views use strongly-typed ViewModels:
- `@model DashboardViewModel` (Home/Index)
- `@model IEnumerable<TenantViewModel>` (Tenants/Index)
- `@model RoomsTabViewModel` (Rooms/Index)
- `@model IEnumerable<PaymentViewModel>` (Payments/Index)
- `@model IEnumerable<MaintenanceRequestViewModel>` (Maintenance/Index)

## Search Keywords for AI Context

### Business Domain Terms
- Property management, rental property, tenant management
- Lease agreements, rent collection, payment tracking
- Maintenance requests, room inspections, utility billing
- Booking system, room availability, occupancy management
- Emergency contacts, tenant profiles, receipt generation

### Technical Terms
- Razor Pages, ASP.NET Core, Entity Framework
- FluentValidation rules, AutoMapper mappings
- Repository pattern, service layer, DTOs
- Authentication, authorization, role-based access
- AJAX modals, Bootstrap components, form validation

### AutoMapper Search Terms
- CreateMap, ReverseMap, ForMember, MapFrom, Ignore
- UserDto, TenantDto, RoomDto, PaymentDto, LeaseAgreementDto
- TenantViewModel, UserViewModel, RoomViewModel, PaymentViewModel, LeaseAgreementViewModel
- CreateTenantDto, UpdateTenantDto, RegisterTenantDto
- MapperConfiguration, MapperConfigurationExpression, IMapper
- AutoMapperMappingException, nested object mapping, navigation properties

### File Patterns
- Controllers: `*Controller.cs` in Web/Controllers/
- Views: `*.cshtml` in Web/Views/
- ViewModels: `*ViewModel.cs` in Web/ViewModels/
- Entities: `*.cs` in Domain/Entities/
- Services: `*ApplicationService.cs` in Application/Services/
- DTOs: `*Dto.cs` in Application/DTOs/
- Validators: `*Validator.cs` in Web/Validators/

## Common Development Patterns

### Service Layer Pattern
```csharp
// Services return ServiceResult<T> for consistent error handling
var result = await _tenantApplicationService.GetTenantByIdAsync(id);
if (!result.IsSuccess) {
    SetErrorMessage(result.ErrorMessage);
    return View(new TenantViewModel());
}
```

### Controller Base Class Usage
```csharp
// All controllers inherit from BaseController for common functionality
public class TenantsController : BaseController
{
    // SetSuccessMessage(), SetErrorMessage(), SetInfoMessage()
}
```

### Modal-Based CRUD Operations
- Add/Edit operations use Bootstrap modals loaded via AJAX
- Partial views return form content for modals
- JavaScript handles modal show/hide and form submission

### FluentValidation Integration
- Each entity has corresponding validator class
- Validation rules enforce business logic and data integrity
- Client and server-side validation coordination

## Security & Authorization

### Role-Based Access
- **Manager Role**: Full system access, tenant management, reporting
- **Tenant Role**: Limited to own profile, payment history, maintenance requests

### Authentication Flow
- Cookie-based authentication via `TenantsController.Login()`
- Claims-based principal with UserId and Role claims
- Automatic redirects based on user role after login

## Data Validation Rules Summary

### Key Business Rules
- South African cellphone format validation (0821234567)
- Unique constraints (room numbers, usernames, tenant-room assignments)
- Date validations (no future dates for historical data)
- Referential integrity (tenants must exist for payments)
- Status constraints (room status must be valid enum values)

### Validation Locations
1. **Client-side**: jQuery validation for immediate feedback
2. **Server-side**: FluentValidation in controller actions
3. **Database**: Entity constraints and relationships

## Testing Strategy

### Unit Test Coverage
- Controller action testing with mocked dependencies
- Service layer business logic validation
- Repository pattern testing with in-memory database
- Validation rule testing for all business entities

### Test Patterns
```csharp
// Arrange: Setup test data and mocks
var mockService = new Mock<ITenantApplicationService>();
var controller = new TenantsController(mockService.Object, mapper);

// Act: Execute the action
var result = await controller.Index();

// Assert: Verify expected behavior
var viewResult = Assert.IsType<ViewResult>(result);
Assert.IsAssignableFrom<IEnumerable<TenantViewModel>>(viewResult.Model);
```

## Common Code Search Scenarios

### Finding AutoMapper Configurations
Search terms: `CreateMap`, `ForMember`, `MapFrom`, `Ignore`, `ReverseMap`

### Finding View-Controller Relationships
Search terms: `@model`, `ViewModel`, `Controller`, `View(model)`

### Finding Business Logic
Search terms: `ApplicationService`, `ServiceResult`, `FluentValidation`

### Finding Data Access
Search terms: `Repository`, `Entity Framework`, `DbContext`, `async`

### Finding UI Components
Search terms: `Bootstrap`, `modal`, `AJAX`, `partial view`

### Finding Validation Rules
Search terms: `Validator`, `RuleFor`, `WithMessage`, `validation`

## Configuration & Environment

### Database Connection
- Entity Framework with SQL Server
- Connection string in appsettings.json
- Migrations for schema management

### Dependency Injection Setup
- Services registered in Program.cs
- Repository pattern with generic interface
- Application services layer for business logic

### Static File Handling
- wwwroot for client-side assets
- File uploads for lease documents and payment proofs
- Receipt generation and download functionality

## Performance & Monitoring

### Prometheus Metrics
- Payment creation counters
- Application performance monitoring
- Custom business metrics tracking

### Logging
- Structured logging with Microsoft.Extensions.Logging
- Error tracking and debugging information
- Performance monitoring for database operations

## Future Enhancement Areas

### Potential Improvements
- Automated rent reminder notifications
- Advanced reporting and analytics dashboard
- Mobile-responsive enhancements
- API endpoints for external integrations
- Document management system improvements
- Multi-property support for property management companies

---

**Last Updated**: December 2024
**Primary AI Use Cases**: Code search, architecture questions, business logic understanding, debugging assistance, feature development guidance, AutoMapper configuration troubleshooting

## Recent AutoMapper Fixes (December 2024)
- ✅ Fixed `UserDto → UserViewModel` mapping exception
- ✅ Fixed `LeaseAgreementDto → LeaseAgreementViewModel` mapping exception
- ✅ Fixed `PaymentDto → PaymentViewModel` mapping with nested objects
- ✅ Added comprehensive nested object mapping support
- ✅ Updated test configurations to match main application mappings