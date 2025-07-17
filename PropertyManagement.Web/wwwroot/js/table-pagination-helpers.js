/**
 * Common Table Pagination Initializations
 * This file provides pre-configured setups for common scenarios
 */

// Quick setup function for basic pagination
window.setupBasicPagination = function(tableId, options = {}) {
    const defaultOptions = {
        tableSelector: `#${tableId}`,
        itemsPerPage: 10,
        showItemsPerPageSelector: true,
        showInfo: true,
        maxVisiblePages: 5
    };
    
    return new TablePagination({ ...defaultOptions, ...options });
};

// Setup with search
window.setupPaginationWithSearch = function(tableId, searchInputId, options = {}) {
    const defaultOptions = {
        tableSelector: `#${tableId}`,
        searchInputSelector: `#${searchInputId}`,
        itemsPerPage: 10,
        showItemsPerPageSelector: true,
        showInfo: true,
        maxVisiblePages: 5
    };
    
    return new TablePagination({ ...defaultOptions, ...options });
};

// Setup for small tables (less controls)
window.setupSmallTablePagination = function(tableId, options = {}) {
    const defaultOptions = {
        tableSelector: `#${tableId}`,
        itemsPerPage: 5,
        showItemsPerPageSelector: false,
        showInfo: false,
        maxVisiblePages: 3
    };
    
    return new TablePagination({ ...defaultOptions, ...options });
};

// Setup for large datasets
window.setupLargeTablePagination = function(tableId, searchInputId, options = {}) {
    const defaultOptions = {
        tableSelector: `#${tableId}`,
        searchInputSelector: searchInputId ? `#${searchInputId}` : null,
        itemsPerPage: 25,
        showItemsPerPageSelector: true,
        showInfo: true,
        maxVisiblePages: 7
    };
    
    return new TablePagination({ ...defaultOptions, ...options });
};

// Common event handlers
window.PaginationHelpers = {
    // Export visible rows to CSV
    exportVisibleToCSV: function(tableId, filename = 'export.csv') {
        const table = document.getElementById(tableId);
        if (!table) return;

        const visibleRows = Array.from(table.querySelectorAll('tbody tr'))
            .filter(row => row.style.display !== 'none');
        
        const headers = Array.from(table.querySelectorAll('thead th'))
            .map(th => th.textContent.trim());
        
        let csvContent = headers.join(',') + '\n';
        
        visibleRows.forEach(row => {
            const cells = Array.from(row.querySelectorAll('td'))
                .map(td => `"${td.textContent.trim().replace(/"/g, '""')}"`);
            csvContent += cells.join(',') + '\n';
        });

        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);

        if (typeof toastr !== 'undefined') {
            toastr.success(`Exported ${visibleRows.length} rows to ${filename}`);
        }
    },

    // Print visible rows
    printVisibleRows: function(tableId, title = 'Table Data') {
        const table = document.getElementById(tableId);
        if (!table) return;

        const printWindow = window.open('', '_blank');
        const tableClone = table.cloneNode(true);
        
        // Hide pagination-related elements
        Array.from(tableClone.querySelectorAll('tbody tr'))
            .forEach(row => {
                if (row.style.display === 'none') {
                    row.remove();
                }
            });

        printWindow.document.write(`
            <!DOCTYPE html>
            <html>
            <head>
                <title>${title}</title>
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
                <style>
                    @media print {
                        .no-print { display: none !important; }
                        table { font-size: 12px; }
                    }
                    body { padding: 20px; }
                </style>
            </head>
            <body>
                <h2>${title}</h2>
                <p>Generated on: ${new Date().toLocaleDateString()}</p>
                ${tableClone.outerHTML}
            </body>
            </html>
        `);
        
        printWindow.document.close();
        printWindow.focus();
        printWindow.print();
    },

    // Refresh pagination after data changes
    refreshPagination: function(tableId) {
        // Find pagination instance and refresh
        const table = document.getElementById(tableId);
        if (table && table._paginationInstance) {
            table._paginationInstance.refresh();
        } else {
            // Re-initialize if no instance found
            window.initTablePagination({ tableSelector: `#${tableId}` });
        }
    }
};

// Auto-enhance common table patterns on page load
document.addEventListener('DOMContentLoaded', function() {
    // Auto-setup for tables with common class names
    const commonSelectors = [
        '.data-table',
        '.list-table', 
        '.report-table'
    ];

    commonSelectors.forEach(selector => {
        const tables = document.querySelectorAll(selector);
        tables.forEach(table => {
            if (!table.hasAttribute('data-pagination') && !table.id) {
                // Generate an ID if none exists
                table.id = `table-${Math.random().toString(36).substr(2, 9)}`;
            }
            
            if (!table.hasAttribute('data-pagination')) {
                // Add basic pagination if not already configured
                table.setAttribute('data-pagination', '');
                table.setAttribute('data-items-per-page', '10');
            }
        });
    });
});