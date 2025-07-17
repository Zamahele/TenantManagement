# Property Management System - AI/Claude Reference Guide

## ?? Context for Future AI Assistants

This document provides essential context for AI assistants working on the Property Management System workspace.

### ?? Workspace Location
`C:\Users\zamajasonn\source\repos\TenantManagement\`

### ??? Architecture Overview
- **.NET 8** Clean Architecture solution
- **Razor Pages** web application (NOT Blazor or MVC)
- **Entity Framework Core** with SQL Server
- **Bootstrap 5** for UI styling
- **Advanced Table Pagination System** implemented

## ?? Key Implementation Patterns

### Table Pagination System ? FULLY IMPLEMENTED
**Status:** Production-ready, tested, and documented

**Core Files:**
- `PropertyManagement.Web/wwwroot/js/table-pagination.js` - Main library
- `PropertyManagement.Web/wwwroot/css/table-pagination.css` - Styling
- `PropertyManagement.Web/Views/Shared/_TableSearch.cshtml` - Search component
- Assets included globally in `_Layout.cshtml`

**Current Implementations:**
- ? Payments table (`Views/Payments/_PaymentsTable.cshtml`)
- ? Inspections table (`Views/Inspections/Index.cshtml`)

**Usage Pattern:**
```html
<!-- Search component (optional) -->
@{
    ViewData["SearchId"] = "table-search";
    ViewData["SearchPlaceholder"] = "Search...";
}
@await Html.PartialAsync("_TableSearch")

<!-- Table with pagination -->
<table id="dataTable" 
       data-pagination 
       data-items-per-page="10"
       data-search-input="#table-search">
    <!-- table content -->
</table>
```

### ?? Build Requirements & Common Issues

**Critical Build Patterns:**
1. **Complete Razor Syntax:** All `@foreach` loops MUST have closing `}`
2. **HTML Completeness:** All tags must be properly closed
3. **ViewData Validation:** Partial views require proper ViewData setup
4. **Table Structure:** Correct colspan values in pagination tables

**Historical Build Failures:**
- Truncated Razor files (incomplete HTML/C# syntax)
- Missing closing braces in foreach loops  
- Incomplete HTML tags (e.g., `<i class="bi bi-rece` instead of `<i class="bi bi-receipt">`)
- Mismatched table column spans

**Validation Process:**
Always run `dotnet build` after making changes to views.

## ??? Project Structure Reference

```
TenantManagement/
??? PropertyManagement.Domain/           # Entities, no dependencies
??? PropertyManagement.Application/      # Services, DTOs, validation
??? PropertyManagement.Infrastructure/   # Data access, EF migrations  
??? PropertyManagement.Web/             # Razor Pages UI (MAIN PROJECT)
?   ??? Controllers/                     # MVC-style controllers
?   ??? Views/                          # Razor views and partials
?   ??? ViewModels/                     # UI-specific models
?   ??? Validators/                     # FluentValidation rules
?   ??? wwwroot/                        # Static assets, pagination files
??? PropertyManagement.Test/            # xUnit tests with Moq
```

## ?? Technology Stack

### Core Dependencies
- **Framework:** .NET 8, C# 12
- **Web:** ASP.NET Core Razor Pages
- **Database:** Entity Framework Core + SQL Server
- **Mapping:** AutoMapper for DTO/ViewModel conversion
- **Validation:** FluentValidation for input validation
- **Testing:** xUnit + Moq for unit/integration tests
- **Monitoring:** Prometheus metrics

### Frontend Stack
- **CSS Framework:** Bootstrap 5 (CDN)
- **Icons:** Bootstrap Icons
- **JavaScript:** jQuery + custom pagination library
- **Notifications:** Toastr.js for user feedback

## ?? UI Patterns

### Standard View Structure
```razor
@model YourViewModel
@{
    ViewData["Title"] = "Page Title";
}

<!-- Page header with actions -->
<div class="card shadow-lg">
    <div class="card-header bg-primary text-white">
        <h2><i class="bi bi-icon"></i> Title</h2>
        <button class="btn btn-light" onclick="action()">
            <i class="bi bi-plus"></i> Add
        </button>
    </div>
    <div class="card-body">
        <!-- Search component -->
        @await Html.PartialAsync("_TableSearch")
        
        <!-- Table with pagination -->
        <table id="table" data-pagination>
            <!-- content -->
        </table>
    </div>
</div>
```

### Modal Integration Pattern
```javascript
function openModal(id = null) {
    const url = id ? `/Controller/Modal?id=${id}` : '/Controller/Modal';
    fetch(url)
        .then(response => response.text())
        .then(html => {
            document.getElementById('modalBody').innerHTML = html;
            new bootstrap.Modal(document.getElementById('modal')).show();
        });
}
```

## ?? Data Flow Patterns

### Request Flow
1. **Controller** receives request
2. **Application Service** handles business logic
3. **Repository** manages data access
4. **AutoMapper** converts between DTOs/ViewModels
5. **FluentValidation** validates input
6. **View** renders with pagination

### Validation Pattern
```csharp
public class EntityValidator : AbstractValidator<EntityViewModel>
{
    public EntityValidator()
    {
        RuleFor(x => x.Property)
            .NotEmpty()
            .MaximumLength(100);
    }
}
```

## ?? Testing Patterns

### Controller Test Structure
```csharp
public class ControllerTests : TestBaseClass
{
    [Fact]
    public async Task Action_ReturnsExpectedResult()
    {
        // Arrange
        var context = GetDbContext();
        var controller = GetController(context);
        
        // Act
        var result = await controller.Action();
        
        // Assert
        Assert.IsType<ViewResult>(result);
    }
}
```

## ?? Future Development Guidelines

### Adding New Paginated Tables
1. Create table with proper HTML structure
2. Add `data-pagination` attribute
3. Include search component if needed
4. Test build and functionality
5. Follow established patterns from Payments/Inspections

### Authentication & Authorization
- Role-based: "Manager" and "Tenant"
- Use `[Authorize(Roles = "Manager")]` for admin features
- Check `User.IsInRole()` in views

### File Upload Patterns
- PDF/image validation implemented
- File size limits enforced
- Secure storage in `wwwroot/uploads/`

## ?? Documentation Locations

1. **Workspace README:** `/README.md` - Overall project documentation
2. **Web App README:** `/PropertyManagement.Web/README.md` - Detailed features
3. **Pagination Guide:** `/PropertyManagement.Web/wwwroot/docs/table-pagination-guide.md`
4. **Quick Template:** `/PropertyManagement.Web/wwwroot/docs/table-pagination-template.md`

## ?? Troubleshooting Quick Reference

### Build Failures
1. Check for incomplete Razor syntax
2. Verify all HTML tags are closed
3. Ensure foreach loops have closing braces
4. Validate ViewData properties for partials

### Pagination Issues
1. Verify table has unique `id` attribute
2. Check data attributes are correct
3. Ensure pagination assets are loaded in layout
4. Validate search input selector if using search

### Common Solutions
- **Truncated files:** Re-edit with complete syntax
- **Missing references:** Check `_Layout.cshtml` includes
- **JavaScript errors:** Verify jQuery is loaded first
- **Styling issues:** Ensure Bootstrap 5 compatibility

---

**Last Updated:** January 2025
**Build Status:** ? Successful
**Pagination Status:** ? Production Ready
**Test Coverage:** ? Comprehensive