/* =============================================================================
   ACCESSIBILITY ENHANCEMENTS AND KEYBOARD NAVIGATION
   =============================================================================
   
   Comprehensive accessibility features including focus management,
   keyboard shortcuts, ARIA announcements, and screen reader support
   
   ============================================================================= */

class AccessibilityManager {
    constructor() {
        this.focusableElements = [];
        this.currentFocusIndex = -1;
        this.isModalOpen = false;
        this.previousFocus = null;
        this.keyboardShortcutsEnabled = true;
        this.announcements = [];
        
        this.init();
    }

    /* =========================================================================
       INITIALIZATION
       ========================================================================= */
    
    init() {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.setupAfterDOMReady());
        } else {
            this.setupAfterDOMReady();
        }
    }

    setupAfterDOMReady() {
        this.createSkipLinks();
        this.setupKeyboardNavigation();
        this.setupFocusManagement();
        this.setupARIAEnhancements();
        this.setupLiveRegions();
        this.setupModalAccessibility();
        this.setupTableAccessibility();
        this.setupFormAccessibility();
        this.setupKeyboardShortcuts();
        this.injectAccessibilityHelpers();
        this.announcePageLoad();
    }

    /* =========================================================================
       SKIP LINKS
       ========================================================================= */
    
    createSkipLinks() {
        const skipLinks = document.createElement('div');
        skipLinks.className = 'skip-links';
        skipLinks.innerHTML = `
            <a href="#main-content" class="skip-link">Skip to main content</a>
            <a href="#sidebar-nav" class="skip-link">Skip to navigation</a>
            <a href="#page-actions" class="skip-link">Skip to page actions</a>
        `;
        
        document.body.insertBefore(skipLinks, document.body.firstChild);
        
        // Add IDs to target elements if they don't exist
        this.addSkipLinkTargets();
    }
    
    addSkipLinkTargets() {
        const mainContent = document.querySelector('.main-content, main') || document.querySelector('#main');
        if (mainContent && !mainContent.id) {
            mainContent.id = 'main-content';
        }
        
        const sidebar = document.querySelector('.sidebar-nav, .sidebar') || document.querySelector('nav');
        if (sidebar && !sidebar.id) {
            sidebar.id = 'sidebar-nav';
        }
        
        const pageActions = document.querySelector('.page-actions');
        if (pageActions && !pageActions.id) {
            pageActions.id = 'page-actions';
        }
    }

    /* =========================================================================
       KEYBOARD NAVIGATION
       ========================================================================= */
    
    setupKeyboardNavigation() {
        this.updateFocusableElements();
        
        document.addEventListener('keydown', (e) => this.handleGlobalKeydown(e));
        
        // Update focusable elements when content changes
        const observer = new MutationObserver(() => {
            this.updateFocusableElements();
        });
        
        observer.observe(document.body, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['disabled', 'aria-hidden', 'tabindex']
        });
    }
    
    updateFocusableElements() {
        const selectors = [
            'button:not([disabled]):not([aria-hidden="true"])',
            'input:not([disabled]):not([type="hidden"]):not([aria-hidden="true"])',
            'select:not([disabled]):not([aria-hidden="true"])',
            'textarea:not([disabled]):not([aria-hidden="true"])',
            'a[href]:not([aria-hidden="true"])',
            '[tabindex]:not([tabindex="-1"]):not([disabled]):not([aria-hidden="true"])',
            '.btn:not([disabled]):not([aria-hidden="true"])',
            '.nav-link:not([aria-hidden="true"])',
            '.dropdown-item:not([disabled]):not([aria-hidden="true"])'
        ];
        
        this.focusableElements = Array.from(document.querySelectorAll(selectors.join(', ')))
            .filter(el => this.isElementVisible(el))
            .sort((a, b) => {
                const aIndex = parseInt(a.getAttribute('tabindex')) || 0;
                const bIndex = parseInt(b.getAttribute('tabindex')) || 0;
                if (aIndex !== bIndex) return aIndex - bIndex;
                
                // Use DOM order for elements with same tabindex
                return Array.prototype.indexOf.call(document.querySelectorAll('*'), a) -
                       Array.prototype.indexOf.call(document.querySelectorAll('*'), b);
            });
    }
    
    isElementVisible(element) {
        const style = window.getComputedStyle(element);
        return style.display !== 'none' && 
               style.visibility !== 'hidden' && 
               element.offsetWidth > 0 && 
               element.offsetHeight > 0;
    }
    
    handleGlobalKeydown(e) {
        // Handle keyboard shortcuts
        if (this.handleKeyboardShortcuts(e)) {
            return;
        }
        
        // Handle modal navigation
        if (this.isModalOpen) {
            this.handleModalKeydown(e);
            return;
        }
        
        // Handle general navigation
        this.handleGeneralNavigation(e);
    }

    /* =========================================================================
       FOCUS MANAGEMENT
       ========================================================================= */
    
    setupFocusManagement() {
        // Track focus for better focus indicators
        document.addEventListener('focusin', (e) => {
            this.currentFocusIndex = this.focusableElements.indexOf(e.target);
            this.addFocusClass(e.target);
        });
        
        document.addEventListener('focusout', (e) => {
            this.removeFocusClass(e.target);
        });
        
        // Enhanced focus visible detection
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                document.body.classList.add('keyboard-nav');
            }
        });
        
        document.addEventListener('mousedown', () => {
            document.body.classList.remove('keyboard-nav');
        });
    }
    
    addFocusClass(element) {
        element.classList.add('focus-visible-enhanced');
    }
    
    removeFocusClass(element) {
        element.classList.remove('focus-visible-enhanced');
    }
    
    focusElement(element) {
        if (element && typeof element.focus === 'function') {
            element.focus();
            this.announceElement(element);
        }
    }
    
    focusFirstFocusableElement() {
        if (this.focusableElements.length > 0) {
            this.focusElement(this.focusableElements[0]);
        }
    }
    
    focusLastFocusableElement() {
        if (this.focusableElements.length > 0) {
            this.focusElement(this.focusableElements[this.focusableElements.length - 1]);
        }
    }

    /* =========================================================================
       ARIA ENHANCEMENTS
       ========================================================================= */
    
    setupARIAEnhancements() {
        this.enhanceButtons();
        this.enhanceDropdowns();
        this.enhanceTabs();
        this.enhanceModals();
        this.enhanceForms();
        this.enhanceTables();
    }
    
    enhanceButtons() {
        const buttons = document.querySelectorAll('button, .btn');
        buttons.forEach(button => {
            // Add aria-pressed for toggle buttons
            if (button.classList.contains('active') && !button.hasAttribute('aria-pressed')) {
                button.setAttribute('aria-pressed', 'true');
            }
            
            // Add descriptive labels if missing
            if (!button.hasAttribute('aria-label') && !button.hasAttribute('aria-labelledby')) {
                const text = this.getElementText(button);
                if (text.trim()) {
                    button.setAttribute('aria-label', text.trim());
                }
            }
        });
    }
    
    enhanceDropdowns() {
        const dropdownToggles = document.querySelectorAll('[data-bs-toggle="dropdown"]');
        dropdownToggles.forEach(toggle => {
            if (!toggle.hasAttribute('aria-expanded')) {
                toggle.setAttribute('aria-expanded', 'false');
            }
            
            if (!toggle.hasAttribute('aria-haspopup')) {
                toggle.setAttribute('aria-haspopup', 'true');
            }
        });
    }
    
    enhanceTabs() {
        const tabLinks = document.querySelectorAll('[data-bs-toggle="tab"]');
        tabLinks.forEach(tab => {
            tab.setAttribute('role', 'tab');
            
            if (!tab.hasAttribute('aria-selected')) {
                tab.setAttribute('aria-selected', tab.classList.contains('active') ? 'true' : 'false');
            }
        });
        
        const tabPanes = document.querySelectorAll('.tab-pane');
        tabPanes.forEach(pane => {
            pane.setAttribute('role', 'tabpanel');
            pane.setAttribute('tabindex', pane.classList.contains('active') ? '0' : '-1');
        });
    }
    
    enhanceModals() {
        const modals = document.querySelectorAll('.modal');
        modals.forEach(modal => {
            modal.setAttribute('role', 'dialog');
            modal.setAttribute('aria-modal', 'true');
            
            if (!modal.hasAttribute('aria-labelledby')) {
                const title = modal.querySelector('.modal-title');
                if (title) {
                    if (!title.id) {
                        title.id = this.generateId('modal-title');
                    }
                    modal.setAttribute('aria-labelledby', title.id);
                }
            }
        });
    }
    
    enhanceForms() {
        const forms = document.querySelectorAll('form');
        forms.forEach(form => {
            const inputs = form.querySelectorAll('input, select, textarea');
            
            inputs.forEach(input => {
                // Associate labels
                const label = form.querySelector(`label[for="${input.id}"]`) || 
                            form.querySelector('label')?.closest('.form-group')?.querySelector('label');
                
                if (label && !input.hasAttribute('aria-labelledby')) {
                    if (!label.id) {
                        label.id = this.generateId('label');
                    }
                    input.setAttribute('aria-labelledby', label.id);
                }
                
                // Add required indicators
                if (input.hasAttribute('required')) {
                    input.setAttribute('aria-required', 'true');
                }
                
                // Add error states
                if (input.classList.contains('is-invalid')) {
                    input.setAttribute('aria-invalid', 'true');
                    const errorElement = form.querySelector('.invalid-feedback');
                    if (errorElement) {
                        if (!errorElement.id) {
                            errorElement.id = this.generateId('error');
                        }
                        input.setAttribute('aria-describedby', errorElement.id);
                    }
                }
            });
        });
    }
    
    enhanceTables() {
        const tables = document.querySelectorAll('table');
        tables.forEach(table => {
            // Add table role if missing
            if (!table.hasAttribute('role')) {
                table.setAttribute('role', 'table');
            }
            
            // Add caption if missing
            if (!table.querySelector('caption')) {
                const tableContainer = table.closest('[class*="table"]');
                const heading = tableContainer?.querySelector('h1, h2, h3, h4, h5, h6');
                if (heading) {
                    const caption = document.createElement('caption');
                    caption.textContent = heading.textContent;
                    caption.className = 'sr-only';
                    table.insertBefore(caption, table.firstChild);
                }
            }
            
            // Enhance headers
            const headers = table.querySelectorAll('th');
            headers.forEach((header, index) => {
                if (!header.hasAttribute('scope')) {
                    header.setAttribute('scope', 'col');
                }
                
                // Add sort indicators for sortable columns
                if (header.querySelector('.sort') || header.classList.contains('sortable')) {
                    header.setAttribute('aria-sort', 'none');
                    header.setAttribute('role', 'columnheader');
                    header.setAttribute('tabindex', '0');
                }
            });
        });
    }

    /* =========================================================================
       LIVE REGIONS
       ========================================================================= */
    
    setupLiveRegions() {
        // Create global live regions
        this.createLiveRegion('polite');
        this.createLiveRegion('assertive');
        
        // Setup form validation announcements
        this.setupFormValidationAnnouncements();
        
        // Setup content change announcements
        this.setupContentChangeAnnouncements();
    }
    
    createLiveRegion(priority) {
        const region = document.createElement('div');
        region.id = `live-region-${priority}`;
        region.setAttribute('aria-live', priority);
        region.setAttribute('aria-atomic', 'true');
        region.className = 'live-region sr-only';
        document.body.appendChild(region);
    }
    
    announce(message, priority = 'polite') {
        const region = document.getElementById(`live-region-${priority}`);
        if (region) {
            region.textContent = message;
            
            // Clear after announcement
            setTimeout(() => {
                region.textContent = '';
            }, 1000);
        }
        
        this.announcements.push({
            message,
            priority,
            timestamp: Date.now()
        });
    }
    
    setupFormValidationAnnouncements() {
        const forms = document.querySelectorAll('form');
        forms.forEach(form => {
            form.addEventListener('submit', (e) => {
                const invalidFields = form.querySelectorAll(':invalid');
                if (invalidFields.length > 0) {
                    this.announce(`Form has ${invalidFields.length} validation errors`, 'assertive');
                }
            });
        });
    }
    
    setupContentChangeAnnouncements() {
        // Announce tab changes
        document.addEventListener('shown.bs.tab', (e) => {
            const tabText = this.getElementText(e.target);
            this.announce(`Switched to ${tabText} tab`);
        });
        
        // Announce modal open/close
        document.addEventListener('shown.bs.modal', (e) => {
            const modalTitle = e.target.querySelector('.modal-title')?.textContent || 'Dialog';
            this.announce(`${modalTitle} dialog opened`);
        });
        
        document.addEventListener('hidden.bs.modal', (e) => {
            this.announce('Dialog closed');
        });
    }

    /* =========================================================================
       MODAL ACCESSIBILITY
       ========================================================================= */
    
    setupModalAccessibility() {
        document.addEventListener('shown.bs.modal', (e) => {
            this.handleModalOpen(e.target);
        });
        
        document.addEventListener('hidden.bs.modal', (e) => {
            this.handleModalClose(e.target);
        });
    }
    
    handleModalOpen(modal) {
        this.isModalOpen = true;
        this.previousFocus = document.activeElement;
        
        // Focus first focusable element in modal
        setTimeout(() => {
            const firstFocusable = modal.querySelector('button, input, select, textarea, [tabindex]:not([tabindex="-1"])');
            if (firstFocusable) {
                firstFocusable.focus();
            }
        }, 100);
        
        // Set up focus trap
        this.setupModalFocusTrap(modal);
    }
    
    handleModalClose(modal) {
        this.isModalOpen = false;
        
        // Restore focus to previous element
        if (this.previousFocus && typeof this.previousFocus.focus === 'function') {
            this.previousFocus.focus();
        }
        
        this.previousFocus = null;
    }
    
    setupModalFocusTrap(modal) {
        const focusableElements = modal.querySelectorAll(
            'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"]):not([disabled])'
        );
        
        const firstFocusable = focusableElements[0];
        const lastFocusable = focusableElements[focusableElements.length - 1];
        
        modal.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                if (e.shiftKey && document.activeElement === firstFocusable) {
                    e.preventDefault();
                    lastFocusable.focus();
                } else if (!e.shiftKey && document.activeElement === lastFocusable) {
                    e.preventDefault();
                    firstFocusable.focus();
                }
            }
        });
    }
    
    handleModalKeydown(e) {
        if (e.key === 'Escape') {
            const openModal = document.querySelector('.modal.show');
            if (openModal) {
                const closeButton = openModal.querySelector('.btn-close, [data-bs-dismiss="modal"]');
                if (closeButton) {
                    closeButton.click();
                }
            }
        }
    }

    /* =========================================================================
       TABLE ACCESSIBILITY
       ========================================================================= */
    
    setupTableAccessibility() {
        const sortableHeaders = document.querySelectorAll('th[aria-sort]');
        
        sortableHeaders.forEach(header => {
            header.addEventListener('click', () => {
                this.handleColumnSort(header);
            });
            
            header.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    this.handleColumnSort(header);
                }
            });
        });
    }
    
    handleColumnSort(header) {
        const currentSort = header.getAttribute('aria-sort');
        let newSort;
        
        switch (currentSort) {
            case 'none':
                newSort = 'ascending';
                break;
            case 'ascending':
                newSort = 'descending';
                break;
            case 'descending':
                newSort = 'none';
                break;
            default:
                newSort = 'ascending';
        }
        
        // Reset other headers
        const table = header.closest('table');
        const allHeaders = table.querySelectorAll('th[aria-sort]');
        allHeaders.forEach(h => {
            if (h !== header) {
                h.setAttribute('aria-sort', 'none');
            }
        });
        
        // Set new sort
        header.setAttribute('aria-sort', newSort);
        
        // Announce sort change
        const columnName = this.getElementText(header);
        const sortDescription = newSort === 'none' ? 'unsorted' : `sorted ${newSort}`;
        this.announce(`${columnName} column ${sortDescription}`);
    }

    /* =========================================================================
       FORM ACCESSIBILITY
       ========================================================================= */
    
    setupFormAccessibility() {
        // Enhanced form validation
        const forms = document.querySelectorAll('form');
        forms.forEach(form => {
            this.enhanceFormValidation(form);
        });
        
        // Live validation feedback
        const inputs = document.querySelectorAll('input, select, textarea');
        inputs.forEach(input => {
            input.addEventListener('blur', () => {
                this.validateField(input);
            });
        });
    }
    
    enhanceFormValidation(form) {
        form.addEventListener('submit', (e) => {
            const isValid = this.validateForm(form);
            if (!isValid) {
                e.preventDefault();
                this.focusFirstInvalidField(form);
            }
        });
    }
    
    validateForm(form) {
        const invalidFields = form.querySelectorAll(':invalid');
        return invalidFields.length === 0;
    }
    
    validateField(field) {
        const isValid = field.checkValidity();
        
        if (isValid) {
            field.classList.remove('is-invalid');
            field.classList.add('is-valid');
            field.setAttribute('aria-invalid', 'false');
        } else {
            field.classList.remove('is-valid');
            field.classList.add('is-invalid');
            field.setAttribute('aria-invalid', 'true');
            
            // Announce validation error
            const errorMessage = field.validationMessage;
            if (errorMessage) {
                this.announce(`Validation error: ${errorMessage}`, 'assertive');
            }
        }
    }
    
    focusFirstInvalidField(form) {
        const firstInvalid = form.querySelector(':invalid');
        if (firstInvalid) {
            firstInvalid.focus();
            firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }

    /* =========================================================================
       KEYBOARD SHORTCUTS
       ========================================================================= */
    
    setupKeyboardShortcuts() {
        this.createKeyboardShortcutsPanel();
        
        // Global shortcuts
        this.shortcuts = {
            'h': 'Go to homepage',
            '/': 'Focus search',
            '?': 'Show keyboard shortcuts',
            'Escape': 'Close modals/dropdowns',
            'Alt+1': 'Go to tenants',
            'Alt+2': 'Go to rooms', 
            'Alt+3': 'Go to payments',
            'Alt+4': 'Go to maintenance',
            'Ctrl+Shift+D': 'Toggle dark mode'
        };
    }
    
    handleKeyboardShortcuts(e) {
        if (!this.keyboardShortcutsEnabled) return false;
        
        // Don't handle shortcuts when typing in inputs
        if (e.target.matches('input, textarea, select, [contenteditable]')) {
            return false;
        }
        
        const key = e.key.toLowerCase();
        const hasModifier = e.ctrlKey || e.altKey || e.metaKey;
        
        // Single key shortcuts
        if (!hasModifier) {
            switch (key) {
                case 'h':
                    e.preventDefault();
                    this.navigateToPage('/');
                    return true;
                case '/':
                    e.preventDefault();
                    this.focusSearch();
                    return true;
                case '?':
                    e.preventDefault();
                    this.toggleKeyboardShortcuts();
                    return true;
            }
        }
        
        // Alt + number shortcuts
        if (e.altKey && !e.ctrlKey && !e.metaKey) {
            switch (key) {
                case '1':
                    e.preventDefault();
                    this.navigateToPage('/Tenants');
                    return true;
                case '2':
                    e.preventDefault();
                    this.navigateToPage('/Rooms');
                    return true;
                case '3':
                    e.preventDefault();
                    this.navigateToPage('/Payments');
                    return true;
                case '4':
                    e.preventDefault();
                    this.navigateToPage('/Maintenance');
                    return true;
            }
        }
        
        return false;
    }
    
    createKeyboardShortcutsPanel() {
        const panel = document.createElement('div');
        panel.id = 'keyboard-shortcuts-panel';
        panel.className = 'keyboard-shortcuts';
        panel.setAttribute('aria-hidden', 'true');
        panel.innerHTML = `
            <h3>Keyboard Shortcuts</h3>
            <div class="shortcuts-list">
                ${Object.entries(this.shortcuts).map(([key, description]) => `
                    <div class="keyboard-shortcut">
                        <span class="keyboard-shortcut-key">${key}</span>
                        <span>${description}</span>
                    </div>
                `).join('')}
            </div>
            <button class="btn btn-sm btn-secondary mt-3" onclick="this.closest('.keyboard-shortcuts').classList.remove('show'); this.closest('.keyboard-shortcuts').setAttribute('aria-hidden', 'true');">Close</button>
        `;
        
        document.body.appendChild(panel);
    }
    
    toggleKeyboardShortcuts() {
        const panel = document.getElementById('keyboard-shortcuts-panel');
        if (panel) {
            const isVisible = panel.classList.contains('show');
            
            if (isVisible) {
                panel.classList.remove('show');
                panel.setAttribute('aria-hidden', 'true');
            } else {
                panel.classList.add('show');
                panel.setAttribute('aria-hidden', 'false');
                panel.querySelector('button').focus();
            }
        }
    }

    /* =========================================================================
       NAVIGATION HELPERS
       ========================================================================= */
    
    navigateToPage(url) {
        window.location.href = url;
    }
    
    focusSearch() {
        const searchInput = document.querySelector('.search-input, input[type="search"], #search');
        if (searchInput) {
            searchInput.focus();
            this.announce('Search focused');
        }
    }
    
    handleGeneralNavigation(e) {
        // Arrow key navigation for certain components
        if (e.target.matches('[role="tab"]')) {
            this.handleTabNavigation(e);
        } else if (e.target.matches('[role="menuitem"], .dropdown-item')) {
            this.handleMenuNavigation(e);
        }
    }
    
    handleTabNavigation(e) {
        if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
            e.preventDefault();
            const tabs = Array.from(document.querySelectorAll('[role="tab"]'));
            const currentIndex = tabs.indexOf(e.target);
            const nextIndex = e.key === 'ArrowLeft' 
                ? (currentIndex - 1 + tabs.length) % tabs.length
                : (currentIndex + 1) % tabs.length;
            
            tabs[nextIndex].focus();
            tabs[nextIndex].click();
        }
    }
    
    handleMenuNavigation(e) {
        const menu = e.target.closest('.dropdown-menu');
        if (!menu) return;
        
        const items = Array.from(menu.querySelectorAll('.dropdown-item:not([disabled])'));
        const currentIndex = items.indexOf(e.target);
        
        let nextIndex;
        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                nextIndex = (currentIndex + 1) % items.length;
                items[nextIndex].focus();
                break;
            case 'ArrowUp':
                e.preventDefault();
                nextIndex = (currentIndex - 1 + items.length) % items.length;
                items[nextIndex].focus();
                break;
            case 'Home':
                e.preventDefault();
                items[0].focus();
                break;
            case 'End':
                e.preventDefault();
                items[items.length - 1].focus();
                break;
        }
    }

    /* =========================================================================
       UTILITY METHODS
       ========================================================================= */
    
    getElementText(element) {
        return element.textContent || element.innerText || element.value || element.alt || '';
    }
    
    generateId(prefix = 'a11y') {
        return `${prefix}-${Math.random().toString(36).substr(2, 9)}`;
    }
    
    announceElement(element) {
        const text = this.getElementText(element);
        const role = element.getAttribute('role') || element.tagName.toLowerCase();
        const label = element.getAttribute('aria-label') || text;
        
        if (label) {
            this.announce(`${role}: ${label}`);
        }
    }
    
    announcePageLoad() {
        setTimeout(() => {
            const title = document.title;
            const mainHeading = document.querySelector('h1');
            const headingText = mainHeading ? mainHeading.textContent : '';
            
            if (headingText) {
                this.announce(`Page loaded: ${headingText}`);
            } else if (title) {
                this.announce(`Page loaded: ${title}`);
            }
        }, 1000);
    }
    
    injectAccessibilityHelpers() {
        const styles = document.createElement('style');
        styles.textContent = `
            .focus-visible-enhanced {
                outline: 3px solid var(--color-primary-500) !important;
                outline-offset: 2px !important;
                box-shadow: 0 0 0 1px var(--bg-surface), 0 0 0 5px rgba(59, 130, 246, 0.3) !important;
            }
            
            .keyboard-nav .focus-visible-enhanced {
                animation: focusPulse 0.3s ease-in-out;
            }
            
            @keyframes focusPulse {
                0% { outline-width: 1px; }
                50% { outline-width: 4px; }
                100% { outline-width: 3px; }
            }
        `;
        
        document.head.appendChild(styles);
    }

    /* =========================================================================
       PUBLIC API
       ========================================================================= */
    
    setKeyboardShortcutsEnabled(enabled) {
        this.keyboardShortcutsEnabled = enabled;
    }
    
    announceMessage(message, priority = 'polite') {
        this.announce(message, priority);
    }
    
    focusElementById(id) {
        const element = document.getElementById(id);
        if (element) {
            this.focusElement(element);
        }
    }
    
    getAnnouncementHistory() {
        return this.announcements.slice();
    }
}

/* =============================================================================
   INITIALIZATION
   ============================================================================= */

let accessibilityManager;

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        accessibilityManager = new AccessibilityManager();
    });
} else {
    accessibilityManager = new AccessibilityManager();
}

// Export for global access
window.AccessibilityManager = AccessibilityManager;
window.accessibility = () => accessibilityManager;