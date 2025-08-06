/* =============================================================================
   DATATABLES DEBUG SCRIPT
   =============================================================================
   
   Simple debug script to troubleshoot DataTables pagination issues
   
   ============================================================================= */

$(document).ready(function() {
    console.log('DataTables Debug: Starting...');
    
    // Check if jQuery and DataTables are loaded
    console.log('jQuery loaded:', typeof $ !== 'undefined');
    console.log('DataTables loaded:', typeof $.fn.DataTable !== 'undefined');
    
    // Find all tables with data-datatable attribute
    const datatables = $('table[data-datatable]');
    console.log('Found tables with data-datatable:', datatables.length);
    
    datatables.each(function(index) {
        const table = $(this);
        const tableId = table.attr('id') || `table-${index}`;
        
        console.log(`Processing table: ${tableId}`);
        console.log('Table rows:', table.find('tbody tr').length);
        
        // Check if already initialized
        if ($.fn.DataTable.isDataTable(table)) {
            console.log(`${tableId} already initialized`);
            const dt = table.DataTable();
            console.log('DataTable info:', dt.page.info());
            
            // Check pagination elements
            setTimeout(() => {
                const wrapper = table.closest('.dataTables_wrapper');
                const paginate = wrapper.find('.dataTables_paginate');
                const buttons = paginate.find('.paginate_button');
                
                console.log(`${tableId} - Wrapper found:`, wrapper.length > 0);
                console.log(`${tableId} - Pagination container found:`, paginate.length > 0);
                console.log(`${tableId} - Pagination buttons found:`, buttons.length);
                console.log(`${tableId} - Pagination visible:`, paginate.is(':visible'));
                console.log(`${tableId} - Pagination HTML:`, paginate.html());
            }, 500);
        } else {
            console.log(`${tableId} not initialized, initializing now...`);
            
            // Initialize with basic config
            try {
                const dt = table.DataTable({
                    pageLength: 10,
                    lengthMenu: [[5, 10, 25, 50], [5, 10, 25, 50]],
                    responsive: true,
                    paging: true,
                    searching: true,
                    ordering: true,
                    info: true,
                    autoWidth: false,
                    pagingType: "full_numbers",
                    language: {
                        paginate: {
                            first: "First",
                            last: "Last",
                            next: "Next",
                            previous: "Previous"
                        }
                    },
                    drawCallback: function(settings) {
                        console.log(`${tableId} - Draw callback executed`);
                        
                        const wrapper = $(this).closest('.dataTables_wrapper');
                        const paginate = wrapper.find('.dataTables_paginate');
                        const buttons = paginate.find('.paginate_button');
                        
                        console.log(`${tableId} - After draw - Buttons:`, buttons.length);
                        
                        // Force show pagination
                        paginate.show();
                        buttons.show();
                        
                        // Add custom classes
                        buttons.addClass('btn btn-sm btn-outline-primary');
                        buttons.filter('.current').removeClass('btn-outline-primary').addClass('btn-primary');
                    }
                });
                
                console.log(`${tableId} initialized successfully`);
                
                // Debug after initialization
                setTimeout(() => {
                    const info = dt.page.info();
                    console.log(`${tableId} - Page info:`, info);
                    
                    const wrapper = table.closest('.dataTables_wrapper');
                    const paginate = wrapper.find('.dataTables_paginate');
                    
                    console.log(`${tableId} - Pagination after init:`, {
                        container: paginate.length,
                        visible: paginate.is(':visible'),
                        display: paginate.css('display'),
                        opacity: paginate.css('opacity'),
                        visibility: paginate.css('visibility')
                    });
                }, 1000);
                
            } catch (error) {
                console.error(`Error initializing ${tableId}:`, error);
            }
        }
    });
    
    // Global check after 2 seconds
    setTimeout(() => {
        console.log('=== FINAL DEBUG CHECK ===');
        $('.dataTables_paginate').each(function() {
            const paginate = $(this);
            const buttons = paginate.find('.paginate_button');
            console.log('Pagination element:', {
                visible: paginate.is(':visible'),
                display: paginate.css('display'),
                opacity: paginate.css('opacity'),
                visibility: paginate.css('visibility'),
                buttons: buttons.length,
                html: paginate.html()
            });
        });
    }, 2000);
});