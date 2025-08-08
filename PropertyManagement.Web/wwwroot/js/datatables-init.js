/**
 * Global DataTables Initialization Script
 * Automatically initializes DataTables on any table with 'data-datatable' attribute
 */

$(document).ready(function() {
    // Small delay to ensure DOM is fully ready
    setTimeout(function() {
        // Initialize visible tables first
        $('table[data-datatable]:visible').each(function() {
            var $table = $(this);
            // Ensure table is fully rendered before initializing
            if ($table.is(':visible') && $table.find('thead').length > 0) {
                initializeDataTable($table);
            }
        });
    }, 100);
    
    // Handle Bootstrap tab events to initialize tables when tabs are shown
    $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
        // Find tables in the newly shown tab
        var targetTab = $(e.target.getAttribute('data-bs-target'));
        targetTab.find('table[data-datatable]').each(function() {
            var $table = $(this);
            var tableId = $table.attr('id');
            
            if (!$.fn.DataTable.isDataTable('#' + tableId)) {
                // Initialize DataTable for tables that haven't been initialized yet
                initializeDataTable($table);
            } else {
                // Just recalculate columns for already initialized tables
                var dataTable = $table.DataTable();
                setTimeout(function() {
                    dataTable.columns.adjust().responsive.recalc();
                }, 10);
            }
        });
    });
});


function initializeDataTable($table) {
    var tableId = $table.attr('id');
    
    // Get configuration from data attributes
    var config = {
        pageLength: parseInt($table.data('page-length')) || 10,
        lengthMenu: [[5, 10, 15, 25, 50], [5, 10, 15, 25, 50]],
        responsive: true,
        paging: true,
        pagingType: "full_numbers",
        order: getDefaultOrder($table),
        columnDefs: getColumnDefs($table),
        // Additional configuration for better error handling
        processing: false,
        deferRender: false,
        autoWidth: false,
        language: {
            search: $table.data('search-label') || "Search:",
            lengthMenu: "Show _MENU_ entries per page",
            info: "Showing _START_ to _END_ of _TOTAL_ entries",
            emptyTable: $table.data('empty-message') || "No data available",
            paginate: {
                first: "First",
                last: "Last", 
                next: "Next",
                previous: "Previous"
            }
        },
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
             '<"row"<"col-sm-12"tr>>' +
             '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        drawCallback: function(settings) {
            // Force pagination button styling after each draw
            setTimeout(function() {
                $('.dataTables_paginate .paginate_button').each(function() {
                    if (!$(this).hasClass('current') && !$(this).hasClass('disabled')) {
                        $(this).addClass('btn btn-outline-primary btn-sm');
                    } else if ($(this).hasClass('current')) {
                        $(this).addClass('btn btn-primary btn-sm');
                    }
                });
                
                // Force table overflow after each draw to support dropdown portals
                $('.dataTables_wrapper, .dataTables_scroll, .dataTables_scrollBody, .table-responsive').css({
                    'overflow': 'visible !important',
                    'overflow-x': 'visible !important',
                    'overflow-y': 'visible !important'
                });
            }, 50);
        }
    };
    
    // Initialize DataTable with error handling
    try {
        if ($.fn.DataTable.isDataTable('#' + tableId)) {
            $('#' + tableId).DataTable().destroy();
        }
        
        // Validate table structure before initialization
        if (validateTableStructure($table)) {
            $table.DataTable(config);
        } else {
            console.warn('Table ' + tableId + ' has invalid structure, skipping DataTable initialization');
        }
    } catch (error) {
        console.error('DataTables initialization failed for table ' + tableId + ':', error);
        // Fallback: Remove data-datatable attribute to prevent further attempts
        $table.removeAttr('data-datatable');
    }
}

/**
 * Validate table structure before DataTables initialization
 */
function validateTableStructure($table) {
    // Check if table has thead and tbody
    if ($table.find('thead').length === 0) {
        console.warn('Table missing thead element');
        return false;
    }
    
    if ($table.find('tbody').length === 0) {
        console.warn('Table missing tbody element');
        return false;
    }
    
    var headerCells = $table.find('thead tr').first().find('th, td').length;
    var bodyRows = $table.find('tbody tr');
    
    // If no body rows, that's valid for DataTables
    if (bodyRows.length === 0) {
        return true;
    }
    
    var hasValidRows = true;
    
    // Check if all body rows have the correct number of cells
    bodyRows.each(function() {
        var $row = $(this);
        var bodyCells = $row.find('td, th').length;
        var totalColspan = 0;
        
        // Calculate total cells including colspan
        $row.find('td, th').each(function() {
            var cellColspan = parseInt($(this).attr('colspan') || '1');
            totalColspan += cellColspan;
        });
        
        // Allow rows with colspan to match header count, or single cell with full colspan for empty states
        if (bodyCells === 1 && totalColspan === headerCells) {
            // This is likely an empty state row, skip validation for DataTables compatibility
            return true;
        }
        else if (bodyCells !== headerCells && totalColspan !== headerCells) {
            console.warn('Table structure issue - Row has ' + bodyCells + ' cells (total colspan: ' + totalColspan + ') but header has ' + headerCells + ' cells');
            hasValidRows = false;
            return false; // break
        }
    });
    
    // Additional check: if there's only one row and it has complex nested content (empty state), 
    // we might want to defer DataTables initialization
    if (bodyRows.length === 1) {
        var firstRowFirstCell = bodyRows.first().find('td').first();
        if (firstRowFirstCell.find('.empty-state').length > 0) {
            console.log('Table appears to have empty state content, DataTables may not be necessary');
            return false; // Don't initialize DataTables for empty state tables
        }
    }
    
    return hasValidRows;
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

// Dropdown positioning code removed - using simple button groups instead