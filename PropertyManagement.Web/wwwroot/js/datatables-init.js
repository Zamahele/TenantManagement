/**
 * Global DataTables Initialization Script
 * Automatically initializes DataTables on any table with 'data-datatable' attribute
 */

$(document).ready(function() {
    // Initialize visible tables immediately
    initializeVisibleTables();
    
    // Handle Bootstrap tab events to initialize DataTables when tabs are shown
    $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
        // Find tables in the newly shown tab
        var targetTab = $(e.target.getAttribute('data-bs-target'));
        targetTab.find('table[data-datatable]').each(function() {
            var $table = $(this);
            var tableId = $table.attr('id');
            
            // Only initialize if not already initialized
            if (!$.fn.DataTable.isDataTable('#' + tableId)) {
                initializeDataTable($table);
            } else {
                // Recalculate column sizing after tab is shown
                $table.DataTable().columns.adjust().responsive.recalc();
            }
        });
    });
});

function initializeVisibleTables() {
    // Find all visible tables with data-datatable attribute
    $('table[data-datatable]:visible').each(function() {
        var $table = $(this);
        initializeDataTable($table);
    });
}

function initializeDataTable($table) {
    var tableId = $table.attr('id');
    
    // Get configuration from data attributes
    var config = {
        pageLength: parseInt($table.data('page-length')) || 10,
        lengthMenu: [[5, 10, 15, 25, 50], [5, 10, 15, 25, 50]],
        responsive: true,
        order: getDefaultOrder($table),
        columnDefs: getColumnDefs($table),
        language: {
            search: $table.data('search-label') || "Search:",
            lengthMenu: "Show _MENU_ entries per page",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            emptyTable: $table.data('empty-message') || "No data available"
        }
    };
    
    // Initialize DataTable
    if ($.fn.DataTable.isDataTable('#' + tableId)) {
        $('#' + tableId).DataTable().destroy();
    }
    
    $table.DataTable(config);
}

/**
 * Get default sort order from data attribute or use date column if found
 */
function getDefaultOrder($table) {
    var defaultOrder = $table.data('default-order');
    if (defaultOrder) {
        return [[defaultOrder.column, defaultOrder.dir]];
    }
    
    // Auto-detect date column for descending sort
    var dateColumnIndex = -1;
    $table.find('thead th').each(function(index) {
        var headerText = $(this).text().toLowerCase();
        if (headerText.includes('date') || headerText.includes('created') || headerText.includes('updated')) {
            dateColumnIndex = index;
            return false; // break
        }
    });
    
    return dateColumnIndex >= 0 ? [[dateColumnIndex, 'desc']] : [[0, 'asc']];
}

/**
 * Get column definitions - disable sorting/searching for action columns
 */
function getColumnDefs($table) {
    var columnDefs = [];
    
    $table.find('thead th').each(function(index) {
        var headerText = $(this).text().toLowerCase();
        if (headerText.includes('action') || headerText.includes('edit') || headerText.includes('delete')) {
            columnDefs.push({
                targets: [index],
                orderable: false,
                searchable: false
            });
        }
    });
    
    return columnDefs;
}