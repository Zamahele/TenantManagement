# Table Pagination Template

This is a quick reference template showing how to add pagination to any existing table view.

## Step 1: Add Search Component (Optional)

```razor
@{
    ViewData["SearchId"] = "your-table-search";
    ViewData["SearchPlaceholder"] = "Search your data...";
    ViewData["SearchLabel"] = "Search";
    // Optional: Add extra action buttons
    ViewData["ShowExtraActions"] = true;
    ViewData["ExtraActionsContent"] = "<button class='btn btn-outline-secondary btn-sm' onclick='exportData()'><i class='bi bi-download'></i> Export</button>";
}
@await Html.PartialAsync("_TableSearch")
```

## Step 2: Convert Your Table

Add pagination attributes to your existing table:

```razor
<table class="table table-striped table-hover paginated-table" 
       id="yourDataTable"
       data-pagination
       data-items-per-page="15"
       data-search-input="#your-table-search"
       data-max-visible-pages="7">
    <thead>
        <tr>
            <th>Column 1</th>
            <th>Column 2</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model)
        {
            <tr>
                <td>@item.Property1</td>
                <td>@item.Property2</td>
                <td>
                    <button class="btn btn-sm btn-primary">Edit</button>
                </td>
            </tr>
        }
    </tbody>
</table>
```

## Step 3: Add JavaScript (Optional)

```javascript
@section Scripts {
    <script>
        function exportData() {
            const table = document.getElementById('yourDataTable');
            const visibleRows = Array.from(table.querySelectorAll('tbody tr'))
                .filter(row => row.style.display !== 'none');
            
            toastr.info(`Exporting ${visibleRows.length} visible records...`);
        }

        function refreshData() {
            location.reload();
        }
    </script>
}
```

## Quick Conversion Checklist

- ? Add search component (optional)
- ? Add id to your table
- ? Add `data-pagination` attribute
- ? Add `data-search-input` if using search
- ? Customize `data-items-per-page` if needed
- ? Add `paginated-table` class for styling
- ? Test functionality

## Data Attributes Reference

| Attribute | Description | Example |
|-----------|-------------|---------|
| `data-pagination` | Enables auto-initialization | `data-pagination` |
| `data-items-per-page` | Default items per page | `data-items-per-page="25"` |
| `data-search-input` | Search input selector | `data-search-input="#my-search"` |
| `data-max-visible-pages` | Max page numbers | `data-max-visible-pages="7"` |
| `data-show-items-selector` | Show items per page dropdown | `data-show-items-selector="false"` |
| `data-show-info` | Show pagination info | `data-show-info="false"` |