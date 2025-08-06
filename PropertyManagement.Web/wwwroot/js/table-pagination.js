/**
 * Table Pagination Script
 * Provides client-side pagination functionality for HTML tables
 * Compatible with Bootstrap 5 styling
 */

class TablePagination {
    constructor(options) {
        this.tableSelector = options.tableSelector || 'table';
        this.itemsPerPage = options.itemsPerPage || 10;
        this.paginationContainer = options.paginationContainer || 'pagination-container';
        this.showItemsPerPageSelector = options.showItemsPerPageSelector !== false; // Default true
        this.showInfo = options.showInfo !== false; // Default true
        this.maxVisiblePages = options.maxVisiblePages || 5;
        this.searchInputSelector = options.searchInputSelector || null;
        
        this.currentPage = 1;
        this.filteredRows = [];
        this.originalRows = [];
        
        // Generate unique IDs for this table
        this.uniqueId = Math.random().toString(36).substr(2, 9);
        this.itemsPerPageId = `items-per-page-${this.uniqueId}`;
        this.paginationInfoId = `pagination-info-${this.uniqueId}`;
        this.paginationNavId = `pagination-nav-${this.uniqueId}`;
        
        this.init();
    }

    init() {
        this.table = document.querySelector(this.tableSelector);
        if (!this.table) {
            console.error('Table not found with selector:', this.tableSelector);
            return;
        }

        this.tbody = this.table.querySelector('tbody');
        if (!this.tbody) {
            console.error('Table body not found');
            return;
        }

        // Store original rows
        this.originalRows = Array.from(this.tbody.querySelectorAll('tr'));
        this.filteredRows = [...this.originalRows];

        this.createPaginationContainer();
        this.setupSearch();
        this.render();
    }

    createPaginationContainer() {
        let container = document.getElementById(this.paginationContainer);
        if (!container) {
            container = document.createElement('div');
            container.id = this.paginationContainer;
            container.className = 'pagination-wrapper mt-3';
            this.table.parentNode.insertBefore(container, this.table.nextSibling);
        }

        container.innerHTML = `
            <div class="row align-items-center">
                <div class="col-md-6">
                    ${this.showInfo ? `<div id="${this.paginationInfoId}" class="text-muted"></div>` : ''}
                </div>
                <div class="col-md-6">
                    <div class="d-flex justify-content-end align-items-center gap-3">
                        ${this.showItemsPerPageSelector ? `
                            <div class="d-flex align-items-center">
                                <label for="${this.itemsPerPageId}" class="form-label me-2 mb-0">Show:</label>
                                <select id="${this.itemsPerPageId}" class="form-select form-select-sm" style="width: auto;">
                                    <option value="5" ${this.itemsPerPage === 5 ? 'selected' : ''}>5</option>
                                    <option value="10" ${this.itemsPerPage === 10 ? 'selected' : ''}>10</option>
                                    <option value="15" ${this.itemsPerPage === 15 ? 'selected' : ''}>15</option>
                                    <option value="25" ${this.itemsPerPage === 25 ? 'selected' : ''}>25</option>
                                    <option value="50" ${this.itemsPerPage === 50 ? 'selected' : ''}>50</option>
                                    <option value="100" ${this.itemsPerPage === 100 ? 'selected' : ''}>100</option>
                                </select>
                            </div>
                        ` : ''}
                        <nav aria-label="Table pagination">
                            <ul id="${this.paginationNavId}" class="pagination pagination-sm mb-0 pagination-primary"></ul>
                        </nav>
                    </div>
                </div>
            </div>
        `;

        // Setup event listeners
        if (this.showItemsPerPageSelector) {
            const selector = document.getElementById(this.itemsPerPageId);
            if (selector) {
                selector.addEventListener('change', (e) => {
                    const newItemsPerPage = parseInt(e.target.value);
                    this.itemsPerPage = newItemsPerPage;
                    this.currentPage = 1;
                    this.render();
                });
            }
        }
    }

    setupSearch() {
        if (this.searchInputSelector) {
            const searchInput = document.querySelector(this.searchInputSelector);
            if (searchInput) {
                searchInput.addEventListener('input', (e) => {
                    this.filterRows(e.target.value);
                });
            }
        }
    }

    filterRows(searchTerm) {
        if (!searchTerm.trim()) {
            this.filteredRows = [...this.originalRows];
        } else {
            const term = searchTerm.toLowerCase();
            this.filteredRows = this.originalRows.filter(row => {
                const text = row.textContent.toLowerCase();
                return text.includes(term);
            });
        }
        this.currentPage = 1;
        this.render();
    }

    render() {
        this.showCurrentPageRows();
        this.updatePaginationNav();
        this.updateInfo();
    }

    showCurrentPageRows() {
        // Hide all rows first
        this.originalRows.forEach(row => {
            row.style.display = 'none';
        });

        // Calculate pagination
        const startIndex = (this.currentPage - 1) * this.itemsPerPage;
        const endIndex = startIndex + this.itemsPerPage;
        const rowsToShow = this.filteredRows.slice(startIndex, endIndex);

        // Show rows for current page
        rowsToShow.forEach(row => {
            row.style.display = '';
        });

        // Handle empty state
        if (this.filteredRows.length === 0) {
            this.showEmptyState();
        } else {
            this.hideEmptyState();
        }
    }

    showEmptyState() {
        this.hideEmptyState(); // Remove any existing empty state
        
        const emptyRow = document.createElement('tr');
        emptyRow.id = 'empty-state-row';
        emptyRow.innerHTML = `
            <td colspan="100%" class="text-center text-muted py-4">
                <i class="bi bi-search"></i> No records found
            </td>
        `;
        this.tbody.appendChild(emptyRow);
    }

    hideEmptyState() {
        const emptyRow = document.getElementById('empty-state-row');
        if (emptyRow) {
            emptyRow.remove();
        }
    }

    updatePaginationNav() {
        const totalPages = Math.ceil(this.filteredRows.length / this.itemsPerPage);
        const nav = document.getElementById(this.paginationNavId);
        
        if (totalPages <= 1) {
            nav.innerHTML = '';
            return;
        }

        let html = '';

        // Previous button
        html += `
            <li class="page-item ${this.currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${this.currentPage - 1}">
                    <i class="bi bi-chevron-left"></i>
                </a>
            </li>
        `;

        // Page numbers
        const startPage = Math.max(1, this.currentPage - Math.floor(this.maxVisiblePages / 2));
        const endPage = Math.min(totalPages, startPage + this.maxVisiblePages - 1);

        // First page and ellipsis
        if (startPage > 1) {
            html += `<li class="page-item"><a class="page-link" href="#" data-page="1">1</a></li>`;
            if (startPage > 2) {
                html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            }
        }

        // Visible page numbers
        for (let i = startPage; i <= endPage; i++) {
            html += `
                <li class="page-item ${i === this.currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>
            `;
        }

        // Last page and ellipsis
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                html += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            }
            html += `<li class="page-item"><a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a></li>`;
        }

        // Next button
        html += `
            <li class="page-item ${this.currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${this.currentPage + 1}">
                    <i class="bi bi-chevron-right"></i>
                </a>
            </li>
        `;

        nav.innerHTML = html;

        // Add click event listeners
        nav.querySelectorAll('a.page-link').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const page = parseInt(e.target.closest('a').dataset.page);
                if (page && page !== this.currentPage && page >= 1 && page <= totalPages) {
                    this.currentPage = page;
                    this.render();
                }
            });
        });
    }

    updateInfo() {
        if (!this.showInfo) return;

        const info = document.getElementById(this.paginationInfoId);
        if (!info) return;

        const totalItems = this.filteredRows.length;
        const startItem = totalItems === 0 ? 0 : (this.currentPage - 1) * this.itemsPerPage + 1;
        const endItem = Math.min(this.currentPage * this.itemsPerPage, totalItems);

        info.textContent = `Showing ${startItem} to ${endItem} of ${totalItems} entries`;
    }

    // Public methods
    goToPage(page) {
        const totalPages = Math.ceil(this.filteredRows.length / this.itemsPerPage);
        if (page >= 1 && page <= totalPages) {
            this.currentPage = page;
            this.render();
        }
    }

    setItemsPerPage(items) {
        this.itemsPerPage = items;
        this.currentPage = 1;
        this.render();
    }

    refresh() {
        this.originalRows = Array.from(this.tbody.querySelectorAll('tr'));
        this.filteredRows = [...this.originalRows];
        this.currentPage = 1;
        this.render();
    }

    destroy() {
        const container = document.getElementById(this.paginationContainer);
        if (container) {
            container.remove();
        }
        
        // Show all rows
        this.originalRows.forEach(row => {
            row.style.display = '';
        });
    }
}

// Global function for easy initialization
window.initTablePagination = function(options = {}) {
    return new TablePagination(options);
};

// Auto-initialize tables with data-pagination attribute
document.addEventListener('DOMContentLoaded', function() {
    const tables = document.querySelectorAll('table[data-pagination]');
    
    tables.forEach(table => {
        const itemsPerPage = parseInt(table.dataset.itemsPerPage) || 10;
        
        const options = {
            tableSelector: table.id ? `#${table.id}` : table,
            itemsPerPage: itemsPerPage,
            maxVisiblePages: parseInt(table.dataset.maxVisiblePages) || 5,
            showItemsPerPageSelector: table.dataset.showItemsSelector !== 'false',
            showInfo: table.dataset.showInfo !== 'false',
            searchInputSelector: table.dataset.searchInput || null
        };
        
        // Add a small delay to ensure the table is fully rendered
        setTimeout(() => {
            new TablePagination(options);
        }, 100);
    });
});