# Property Management System Workspace

## 🏗️ Workspace Overview

**Location:** `C:\Users\zamajasonn\source\repos\TenantManagement\`

This is a comprehensive .NET 8 property management solution built with Clean Architecture principles, featuring a Razor Pages web application with advanced table pagination and data management capabilities.

## 📁 Project Structure

```
TenantManagement/
├── PropertyManagement.Domain/           # Domain entities and business logic
├── PropertyManagement.Application/      # Application services and DTOs
├── PropertyManagement.Infrastructure/   # Data access and external services
├── PropertyManagement.Web/             # Razor Pages web application (Main UI)
└── PropertyManagement.Test/            # Unit and integration tests
```

### Project Dependencies

| Project | Target Framework | Key Dependencies |
|---------|------------------|------------------|
| **PropertyManagement.Web** | .NET 8 | ASP.NET Core, Entity Framework, AutoMapper, FluentValidation, Bootstrap 5 |
| **PropertyManagement.Application** | .NET 8 | MediatR, AutoMapper, FluentValidation |
| **PropertyManagement.Infrastructure** | .NET 8 | Entity Framework Core, SQL Server |
| **PropertyManagement.Domain** | .NET 8 | Core business entities (no dependencies) |
| **PropertyManagement.Test** | .NET 8 | xUnit, Moq, Entity Framework InMemory |

## 🎯 Key Features Implemented

### ✅ Core Property Management
- Tenant management with user authentication
- Room availability and booking system
- Lease agreement management with file uploads
- Payment tracking with receipt generation
- Maintenance request system
- Inspection scheduling and reporting
- Utility billing and tracking

### ✅ Advanced Table Pagination System
- **Client-side pagination** with configurable page sizes
- **Real-time search functionality** across all table data
- **Responsive design** optimized for mobile devices
- **Export to CSV** functionality for data analysis
- **Print support** for filtered/visible data
- **Auto-initialization** via HTML data attributes
- **Bootstrap 5 integration** with consistent styling

## 🚀 Table Pagination Implementation

### Files Structure
```
PropertyManagement.Web/
├── wwwroot/
│   ├── js/
│   │   ├── table-pagination.js          # Core pagination library
│   │   └── table-pagination-helpers.js  # Helper functions and utilities
│   ├── css/
│   │   └── table-pagination.css         # Pagination styling
│   └── docs/
│       ├── table-pagination-guide.md    # Comprehensive usage guide
│       └── table-pagination-template.md # Quick reference template
├── Views/
│   └── Shared/
│       ├── _Layout.cshtml              # Global layout (includes pagination assets)
│       └── _TableSearch.cshtml         # Reusable search component
```

### Currently Implemented Views
- ✅ **Payments Table** (`Views/Payments/`)
- ✅ **Inspections Table** (`Views/Inspections/`)
- 🔄 Ready for implementation in other views

### Quick Implementation Guide

**1. Basic Table Pagination:**
```html
<table id="myTable" data-pagination data-items-per-page="10">
    <!-- table content -->
</table>
```

**2. With Search:**
```html
@{
    ViewData["SearchId"] = "my-search";
    ViewData["SearchPlaceholder"] = "Search...";
}
@await Html.PartialAsync("_TableSearch")

<table id="myTable" 
       data-pagination 
       data-search-input="#my-search">
    <!-- table content -->
</table>
```

**3. Advanced Configuration:**
```javascript
new TablePagination({
    tableSelector: '#myTable',
    itemsPerPage: 15,
    searchInputSelector: '#search-input',
    maxVisiblePages: 7
});
```

## 🛠️ Development Guidelines

### Build Requirements
- **.NET 8 SDK** (C# 12 language features)
- **SQL Server** (LocalDB for development)
- **Node.js** (for frontend package management)
- **Visual Studio 2022** or **VS Code** with C# extension

### Code Standards
- **Clean Architecture** patterns throughout
- **Repository Pattern** with Unit of Work
- **AutoMapper** for DTO/ViewModel mapping
- **FluentValidation** for input validation
- **xUnit + Moq** for comprehensive testing

### Common Build Issues to Avoid
- ❌ Incomplete Razor syntax (missing closing braces `}`)
- ❌ Truncated HTML tags in views
- ❌ Missing ViewData properties for partial views
- ❌ Incorrect table column spans in pagination tables

## 🧪 Testing Strategy

### Test Coverage
- **Unit Tests:** Controllers, Services, Validators
- **Integration Tests:** Database operations, API endpoints
- **Compilation Tests:** Ensures all projects build successfully

### Test Projects Structure
```
PropertyManagement.Test/
├── Controllers/        # Controller-specific tests
├── Services/          # Application service tests
├── Validators/        # FluentValidation tests
└── TestBaseClass.cs   # Shared test utilities and setup
```

## 📊 Monitoring & Observability

- **Prometheus Metrics:** Payment creation counters and system metrics
- **Structured Logging:** Comprehensive application logging
- **Health Checks:** Database connectivity and system health monitoring

## 🔐 Security Features

- **Role-based Authentication:** Manager and Tenant roles
- **Input Validation:** FluentValidation across all forms
- **File Upload Security:** Restricted file types and size limits
- **SQL Injection Protection:** Entity Framework parameterized queries

## 🎨 UI/UX Features

- **Bootstrap 5** responsive design
- **Bootstrap Icons** for consistent iconography
- **Toastr Notifications** for user feedback
- **Modal Dialogs** for form interactions
- **Table Pagination** with search and export capabilities

## 📝 Validation Rules Summary

The system implements comprehensive validation using FluentValidation:

### Key Entity Validations
- **Tenants:** Name validation, SA phone numbers, unique usernames
- **Payments:** Amount validation, no duplicates, date constraints
- **Rooms:** Unique numbers, status constraints
- **Maintenance:** Description length, status workflow validation

[See PropertyManagement.Web/README.md for complete validation rules]

## 🚀 Getting Started

### 1. Initial Setup
```bash
git clone <repository-url>
cd TenantManagement
dotnet restore
dotnet build
```

### 2. Database Setup
```bash
cd PropertyManagement.Web
dotnet ef database update
```

### 3. Run Application
```bash
dotnet run --project PropertyManagement.Web
```

**Default Login:**
- Username: `Admin`
- Password: `01Pa$$w0rd2025#`

## 📚 Documentation References

- **[Table Pagination Guide](PropertyManagement.Web/wwwroot/docs/table-pagination-guide.md)** - Complete implementation guide
- **[Pagination Template](PropertyManagement.Web/wwwroot/docs/table-pagination-template.md)** - Quick reference
- **[Web Application README](PropertyManagement.Web/README.md)** - Detailed feature documentation

## 🔄 Future Development

### Ready for Implementation
- Additional table pagination in remaining views
- Advanced reporting with pagination
- Bulk operations with table selection
- Real-time updates with SignalR integration

### Architecture Extensions
- API endpoints for mobile applications
- Multi-tenant support
- Advanced caching strategies
- Message queue integration

---

**Last Updated:** January 2025  
**Framework Version:** .NET 8  
**Database:** SQL Server  
**Architecture:** Clean Architecture with Razor Pages