# Property Management Web Application

A comprehensive Razor Pages solution for managing rental properties, tenants, and operations. Built with .NET 8 and C# 12.

## Features

### 1. Maintenance Request Management
- Tenants can submit maintenance requests for their rooms.
- Property managers can assign, track, and update the status of maintenance tasks.
- Maintenance history is logged per room.

### 2. Lease Agreement Management
- Store and manage digital copies of lease agreements.
- Track lease start and end dates, with notifications for upcoming expirations.

### 3. Payment Tracking and Receipts
- Record all rent and deposit payments.
- Generate and send payment receipts to tenants.
- Track outstanding balances and payment history.
- **Prometheus metrics** are used to monitor payment creation events.

### 4. Automated Notifications
- Reminders for tenants about upcoming rent due dates.
- Notifications for property managers about overdue rents or expiring leases.
- Alerts for maintenance staff on new or urgent requests.

### 5. Room Availability and Booking
- Track room status: occupied, vacant, or under maintenance.
- Prospective tenants can view available rooms and submit booking requests.

### 6. Tenant Profile Management
- Maintain detailed tenant profiles (contact info, emergency contacts, rental history).
- Tenants can update their own information.

### 7. Inspection Scheduling and Records
- Schedule regular room inspections.
- Record inspection results and follow-up actions.

### 8. Utility Billing and Tracking
- Track utility usage (water, electricity) per room or tenant.
- Generate and send utility bills.

### 9. Document Management
- Store important property documents (insurance, compliance certificates).

### 10. Reporting and Analytics
- Generate reports on occupancy rates, rent collection, maintenance costs, and more.
- Visual dashboards for property performance.

### 11. Advanced Table Pagination System ?
- **Client-side pagination** with configurable page sizes (5, 10, 25, 50, 100)
- **Real-time search functionality** across all table data
- **Responsive design** optimized for mobile and desktop
- **Export to CSV** functionality for data analysis
- **Print support** for filtered/visible data
- **Auto-initialization** via HTML data attributes
- **Bootstrap 5 integration** with consistent styling

## Table Pagination Implementation

### Features
- ? **Search Integration:** Real-time filtering across all columns
- ? **Export Options:** CSV export of visible/filtered data
- ? **Print Support:** Print visible table data with formatting
- ? **Responsive Design:** Mobile-friendly pagination controls
- ? **Auto-initialization:** Simple data attribute setup
- ? **Empty State Handling:** User-friendly "no results" messages

### Current Implementations
- **Payments Table:** Full pagination with search and export
- **Inspections Table:** Pagination with enhanced search
- **Ready for:** All other data tables in the application

### Quick Usage Examples

**Basic Implementation:**
```html
<table id="myTable" data-pagination data-items-per-page="10">
    <thead>
        <tr><th>Column 1</th><th>Column 2</th></tr>
    </thead>
    <tbody>
        <!-- table rows -->
    </tbody>
</table>
```

**With Search Component:**
```razor
@{
    ViewData["SearchId"] = "my-search";
    ViewData["SearchPlaceholder"] = "Search data...";
}
@await Html.PartialAsync("_TableSearch")

<table id="myTable" 
       data-pagination 
       data-search-input="#my-search"
       data-items-per-page="15">
    <!-- table content -->
</table>
```

**JavaScript Integration:**
```javascript
// Auto-initialization (preferred)
// Just add data-pagination attribute

// Manual initialization (for advanced control)
const pagination = new TablePagination({
    tableSelector: '#myTable',
    itemsPerPage: 20,
    searchInputSelector: '#search',
    maxVisiblePages: 7
});

// Helper functions available
PaginationHelpers.exportVisibleToCSV('myTable', 'export.csv');
PaginationHelpers.printVisibleRows('myTable', 'Report Title');
```

### Files Structure
```
wwwroot/
??? js/
?   ??? table-pagination.js          # Core pagination library
?   ??? table-pagination-helpers.js  # Utilities (export, print, etc.)
??? css/
?   ??? table-pagination.css         # Bootstrap 5 compatible styling
??? docs/
    ??? table-pagination-guide.md    # Complete implementation guide
    ??? table-pagination-template.md # Quick reference template
```

## Observability

- **Prometheus** is used for application metrics (e.g., payment creation counters).
- **OpenTelemetry** has been removed from the project. If you need distributed tracing, you can re-integrate it as needed.

## Testing

- The solution uses **xUnit** and **Moq** for unit testing.
- Example: `PaymentsControllerTests` covers payment creation, editing, deletion, and receipt generation.
- **Compilation Tests:** Ensure all projects build successfully including pagination assets.

## Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB for development)
- Visual Studio 2022 or VS Code

### Setup
```bash
# Clone and restore packages
git clone <repository-url>
dotnet restore
dotnet build

# Setup database
dotnet ef database update

# Run application
dotnet run
```

**Default Login:**
- Username: `Admin`
- Password: `01Pa$$w0rd2025#`

### Adding Pagination to New Tables

1. **Include pagination assets** (already done in `_Layout.cshtml`)
2. **Add search component** (optional):
   ```razor
   @{
       ViewData["SearchId"] = "your-search";
       ViewData["SearchPlaceholder"] = "Search...";
   }
   @await Html.PartialAsync("_TableSearch")
   ```
3. **Add pagination attributes** to your table:
   ```html
   <table id="yourTable" 
          data-pagination 
          data-items-per-page="10"
          data-search-input="#your-search">
   ```
4. **Test and customize** as needed

## Data Validation Rules

This project uses [FluentValidation](https://fluentvalidation.net/) to enforce business and data integrity rules across all major entities and view models. Below is a summary of the key validation rules for each domain area.

### Tenant
- **Full Name:** Required, letters and spaces only, max 100 characters.
- **Contact:** Required, valid South African cellphone (e.g., 0821234567).
- **Room:** Required, must exist, and must not already have an active tenant.
- **Emergency Contact Name:** Required, letters and spaces only, max 100 characters.
- **Emergency Contact Number:** Required, valid South African cellphone.
- **User:** Required, username must be unique.
- **UserId:** Must exist in Users table, not already linked to another tenant.

### Tenant Login
- **Username:** Required.
- **Password:** Required.

### Room
- **Number:** Required, max 10 characters, must be unique.
- **Type:** Required, max 50 characters.
- **Status:** Required, must be one of: Available, Occupied, Under Maintenance.

### Booking Request
- **Room:** Required, must exist, must be available.
- **Full Name:** Required, letters and spaces only, max 100 characters.
- **Contact:** Required, valid South African cellphone.
- **Note:** Optional, max 500 characters.
- **No duplicate pending booking:** Cannot have a pending booking for the same room and contact.
- **Proof of Payment:** If present, must be PDF, JPG, or PNG.

### Payment
- **Tenant:** Required, must exist, must be assigned to a room.
- **Amount:** Required, greater than zero.
- **Type:** Required, must be 'Rent' or 'Deposit'.
- **Month/Year:** Month 1-12, Year 2000-2100.
- **No duplicate payment:** Cannot have a payment for the same tenant, year, and month.
- **Date:** Cannot be in the future.
- **Lease Agreement:** If set, must exist and belong to the tenant.
- **No future period payment:** Cannot record payment for a future period.

### Maintenance Request
- **Room:** Required, must exist.
- **Description:** Required, 10-1000 characters.
- **Status:** Required, must be Pending, In Progress, or Completed.
- **Assigned To:** Required, max 100 characters.
- **Completed Date:** Must be set if status is Completed, must be empty otherwise.
- **Request Date:** Required, not in the future.
- **Tenant:** Required, must be numeric, must exist.

### Lease Agreement
- **Tenant:** Required, must exist.
- **Room:** Required, must exist.
- **Start Date:** Required, not in the past for new agreements.
- **End Date:** Required, must be after start date.
- **Rent Amount:** Required, greater than zero.
- **Expected Rent Day:** 1-28.
- **File:** If present, must be PDF.
- **No overlapping lease:** Only one active lease per tenant per room for a given period.

### Inspection
- **Room:** Required, must exist.
- **Date:** Required, not in the future.
- **Result:** Required, max 200 characters.
- **Notes:** Optional, max 1000 characters.
- **No duplicate inspection:** Cannot have an inspection for the same room and date.

### Utility Bill
- **Room:** Required, must exist.
- **Billing Date:** Required, not in the future.
- **Water Usage:** Zero or positive.
- **Electricity Usage:** Zero or positive.
- **Total Amount:** Zero or positive.
- **Notes:** Optional, max 1000 characters.

## Technical Architecture

### Clean Architecture Layers
- **Domain:** Core business entities and rules
- **Application:** Use cases and business logic
- **Infrastructure:** Data access and external services  
- **Web:** Razor Pages UI with table pagination

### Key Technologies
- **ASP.NET Core 8:** Razor Pages web framework
- **Entity Framework Core:** Data access with SQL Server
- **AutoMapper:** Object-to-object mapping
- **FluentValidation:** Input validation
- **Bootstrap 5:** Responsive UI framework
- **jQuery:** DOM manipulation for pagination
- **Prometheus:** Application metrics
- **xUnit + Moq:** Testing framework

---

**Note:**  
Some rules (like uniqueness and referential integrity) are enforced both in the application and at the database level for maximum reliability.

**Documentation:**
- [Table Pagination Guide](wwwroot/docs/table-pagination-guide.md) - Complete implementation guide
- [Pagination Template](wwwroot/docs/table-pagination-template.md) - Quick reference