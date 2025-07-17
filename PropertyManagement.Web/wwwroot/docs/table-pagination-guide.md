# Table Pagination Script Documentation

## Overview
This pagination system provides client-side pagination for HTML tables with search functionality, items-per-page selection, and responsive design. It's designed to work seamlessly with Bootstrap 5 and the existing PropertyManagement.Web application.

## Features
- ? Client-side pagination with configurable page sizes
- ? Built-in search functionality
- ? Responsive design for mobile devices
- ? Bootstrap 5 compatible styling
- ? Customizable options
- ? Auto-initialization support
- ? Empty state handling
- ? Keyboard navigation support

## Quick Start

### 1. Include Required Files
Add to your layout or view:

```html
<!-- CSS -->
<link rel="stylesheet" href="~/css/table-pagination.css" />

<!-- JavaScript (after jQuery and Bootstrap) -->
<script src="~/js/table-pagination.js"></script>
```

### 2. Auto-initialization (Easiest Method)
Add `data-pagination` attribute to your table:

```html
<table class="table table-striped" 
       id="myTable"
       data-pagination
       data-items-per-page="10"
       data-search-input="#table-search">
    <thead>
        <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        <!-- Your table rows -->
    </tbody>
</table>
```

### 3. Manual Initialization
For more control, initialize programmatically:

```javascript
const pagination = new TablePagination({
    tableSelector: '#myTable',
    itemsPerPage: 10,
    searchInputSelector: '#table-search',
    maxVisiblePages: 5
});
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `tableSelector` | string | `'table'` | CSS selector for the table |
| `itemsPerPage` | number | `10` | Number of rows per page |
| `paginationContainer` | string | `'pagination-container'` | ID for pagination container |
| `showItemsPerPageSelector` | boolean | `true` | Show items per page dropdown |
| `showInfo` | boolean | `true` | Show "Showing X to Y of Z entries" |
| `maxVisiblePages` | number | `5` | Maximum page numbers to show |
| `searchInputSelector` | string | `null` | CSS selector for search input |

## Data Attributes for Auto-initialization

| Attribute | Description | Example |
|-----------|-------------|---------|
| `data-pagination` | Enables auto-initialization | `data-pagination` |
| `data-items-per-page` | Default items per page | `data-items-per-page="25"` |
| `data-max-visible-pages` | Max visible page numbers | `data-max-visible-pages="7"` |
| `data-show-items-selector` | Show items per page selector | `data-show-items-selector="false"` |
| `data-show-info` | Show pagination info | `data-show-info="false"` |
| `data-search-input` | Search input selector | `data-search-input="#my-search"` |

## Complete Example

### HTML Structure
```html
<!-- Search Component (Optional) -->
@{
    ViewData["SearchId"] = "tenant-search";
    ViewData["SearchPlaceholder"] = "Search tenants...";
    ViewData["SearchLabel"] = "Search Tenants";
}
@await Html.PartialAsync("_TableSearch")

<!-- Table with Pagination -->
<div class="card">
    <div class="card-header">
        <h5>Tenants List</h5>
    </div>
    <div class="card-body">
        <table class="table table-striped table-hover paginated-table" 
               id="tenantsTable"
               data-pagination
               data-items-per-page="10"
               data-search-input="#tenant-search">
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Phone</th>
                    <th>Room</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var tenant in Model)
                {
                    <tr>
                        <td>@tenant.FirstName @tenant.LastName</td>
                        <td>@tenant.Email</td>
                        <td>@tenant.PhoneNumber</td>
                        <td>@tenant.RoomNumber</td>
                        <td>
                            <button class="btn btn-sm btn-primary">Edit</button>
                            <button class="btn btn-sm btn-danger">Delete</button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>
```

## Public Methods

```javascript
// Get pagination instance
const pagination = window.myTablePagination;

// Navigate to specific page
pagination.goToPage(3);

// Change items per page
pagination.setItemsPerPage(25);

// Refresh pagination (useful after adding/removing rows)
pagination.refresh();

// Destroy pagination
pagination.destroy();
```

## Styling

The pagination uses Bootstrap 5 classes and includes custom CSS for enhanced appearance:

- Responsive design that adapts to mobile screens
- Hover effects on table rows
- Consistent styling with the application theme
- Dark theme support

## Integration with Existing Views

### For Razor Pages Views
1. Add the CSS and JS files to your layout
2. Use the `_TableSearch` partial for search functionality
3. Add `data-pagination` to your existing tables
4. Optionally customize with data attributes

### For Modal Tables
The pagination works inside modals. Just ensure the table has a unique ID:

```html
<div class="modal-body">
    <table id="modalTable" data-pagination data-items-per-page="5">
        <!-- table content -->
    </table>
</div>
```

## Browser Support
- Modern browsers (Chrome, Firefox, Safari, Edge)
- Internet Explorer 11+ (with polyfills)
- Mobile browsers (iOS Safari, Chrome Mobile)

## Troubleshooting

**Pagination not working?**
- Ensure jQuery is loaded before the pagination script
- Check that the table has a `<tbody>` element
- Verify the table selector is correct

**Search not working?**
- Make sure the search input selector is correct
- Ensure the search input exists in the DOM

**Styling issues?**
- Include the CSS file after Bootstrap
- Check for CSS conflicts

## Performance Notes
- Pagination is client-side only
- All data is loaded initially
- For large datasets (1000+ rows), consider server-side pagination
- Search is case-insensitive and searches all text content in rows