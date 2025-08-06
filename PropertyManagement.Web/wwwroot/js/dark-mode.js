/* =============================================================================
   DARK MODE THEME TOGGLE SYSTEM
   =============================================================================
   
   Complete dark mode implementation with persistent storage, smooth transitions,
   and accessibility considerations for the Property Management application
   
   ============================================================================= */

class DarkModeManager {
    constructor() {
        this.storageKey = 'property-management-theme';
        this.toggleButton = null;
        this.theme = this.getStoredTheme() || this.getPreferredTheme();
        
        this.init();
    }

    /* =========================================================================
       INITIALIZATION
       ========================================================================= */
    
    init() {
        // Apply theme immediately to prevent flash
        this.applyTheme(this.theme);
        
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.setupAfterDOMReady());
        } else {
            this.setupAfterDOMReady();
        }
    }

    setupAfterDOMReady() {
        this.createToggleButton();
        this.setupEventListeners();
        this.updateButtonState();
        
        // Listen for system theme changes
        this.watchSystemTheme();
    }

    /* =========================================================================
       THEME DETECTION AND STORAGE
       ========================================================================= */
    
    getPreferredTheme() {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    }
    
    getStoredTheme() {
        try {
            return localStorage.getItem(this.storageKey);
        } catch (e) {
            console.warn('localStorage not available for theme storage');
            return null;
        }
    }
    
    storeTheme(theme) {
        try {
            localStorage.setItem(this.storageKey, theme);
        } catch (e) {
            console.warn('Could not store theme preference');
        }
    }

    /* =========================================================================
       THEME APPLICATION
       ========================================================================= */
    
    applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        this.theme = theme;
        this.storeTheme(theme);
        
        // Update meta theme-color for mobile browsers
        this.updateMetaThemeColor(theme);
        
        // Dispatch theme change event
        this.dispatchThemeChangeEvent(theme);
    }
    
    updateMetaThemeColor(theme) {
        let themeColor = theme === 'dark' ? '#1e2328' : '#ffffff';
        
        let metaTag = document.querySelector('meta[name="theme-color"]');
        if (!metaTag) {
            metaTag = document.createElement('meta');
            metaTag.setAttribute('name', 'theme-color');
            document.head.appendChild(metaTag);
        }
        metaTag.setAttribute('content', themeColor);
    }
    
    dispatchThemeChangeEvent(theme) {
        const event = new CustomEvent('themeChanged', {
            detail: { theme }
        });
        document.dispatchEvent(event);
    }

    /* =========================================================================
       TOGGLE BUTTON CREATION
       ========================================================================= */
    
    createToggleButton() {
        // Find the navbar actions container
        const navbarActions = document.querySelector('.navbar-actions');
        if (!navbarActions) return;
        
        // Create toggle button container
        const toggleContainer = document.createElement('div');
        toggleContainer.className = 'theme-toggle-container d-flex align-items-center me-3';
        
        // Create the toggle button
        this.toggleButton = document.createElement('button');
        this.toggleButton.className = 'btn btn-theme-toggle btn-sm';
        this.toggleButton.setAttribute('type', 'button');
        this.toggleButton.setAttribute('title', 'Toggle theme');
        this.toggleButton.setAttribute('aria-label', 'Toggle between light and dark theme');
        
        // Add button content
        this.toggleButton.innerHTML = `
            <div class="theme-toggle-icon">
                <i class="bi bi-sun-fill theme-icon theme-icon-light"></i>
                <i class="bi bi-moon-fill theme-icon theme-icon-dark"></i>
            </div>
        `;
        
        // Add button to container and container to navbar
        toggleContainer.appendChild(this.toggleButton);
        navbarActions.insertBefore(toggleContainer, navbarActions.firstChild);
    }

    /* =========================================================================
       EVENT LISTENERS
       ========================================================================= */
    
    setupEventListeners() {
        // Toggle button click
        if (this.toggleButton) {
            this.toggleButton.addEventListener('click', () => this.toggleTheme());
        }
        
        // Keyboard shortcut (Ctrl/Cmd + Shift + D)
        document.addEventListener('keydown', (e) => {
            if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key === 'D') {
                e.preventDefault();
                this.toggleTheme();
                this.showThemeChangeNotification();
            }
        });
        
        // Listen for custom theme change events
        document.addEventListener('themeChanged', (e) => {
            this.onThemeChanged(e.detail.theme);
        });
    }
    
    watchSystemTheme() {
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.addEventListener('change', (e) => {
                // Only change if user hasn't manually set a preference
                if (!this.getStoredTheme()) {
                    const newTheme = e.matches ? 'dark' : 'light';
                    this.applyTheme(newTheme);
                    this.updateButtonState();
                }
            });
        }
    }

    /* =========================================================================
       THEME TOGGLING
       ========================================================================= */
    
    toggleTheme() {
        const newTheme = this.theme === 'light' ? 'dark' : 'light';
        this.applyTheme(newTheme);
        this.updateButtonState();
        this.animateToggle();
    }
    
    updateButtonState() {
        if (!this.toggleButton) return;
        
        const isDark = this.theme === 'dark';
        this.toggleButton.setAttribute('data-theme', this.theme);
        this.toggleButton.setAttribute('aria-pressed', isDark.toString());
        
        // Update title text
        const titleText = isDark ? 'Switch to light theme' : 'Switch to dark theme';
        this.toggleButton.setAttribute('title', titleText);
        this.toggleButton.setAttribute('aria-label', titleText);
    }
    
    animateToggle() {
        if (!this.toggleButton) return;
        
        // Add animation class
        this.toggleButton.classList.add('theme-toggle-animating');
        
        // Remove animation class after animation completes
        setTimeout(() => {
            this.toggleButton.classList.remove('theme-toggle-animating');
        }, 300);
    }

    /* =========================================================================
       EVENT HANDLERS
       ========================================================================= */
    
    onThemeChanged(theme) {
        // Update any theme-dependent components
        this.updateDataTables();
        this.updateToastrTheme();
        this.updateCharts();
    }
    
    updateDataTables() {
        // Redraw DataTables if they exist to apply new theme
        if (window.$ && window.$.fn.DataTable) {
            window.$.fn.dataTable.tables({ visible: true, api: true }).columns.adjust();
        }
    }
    
    updateToastrTheme() {
        // Update toastr theme if available
        if (window.toastr) {
            const isDark = this.theme === 'dark';
            window.toastr.options = {
                ...window.toastr.options,
                positionClass: 'toast-top-right',
                toastClass: isDark ? 'toast-dark' : 'toast-light'
            };
        }
    }
    
    updateCharts() {
        // Update any chart themes (placeholder for future chart implementations)
        const event = new CustomEvent('chartsThemeChanged', {
            detail: { theme: this.theme }
        });
        document.dispatchEvent(event);
    }
    
    showThemeChangeNotification() {
        if (window.toastr) {
            const message = `Switched to ${this.theme} theme`;
            window.toastr.info(message, 'Theme Changed', {
                timeOut: 2000,
                showDuration: 300,
                hideDuration: 300
            });
        }
    }

    /* =========================================================================
       PUBLIC API
       ========================================================================= */
    
    setTheme(theme) {
        if (theme === 'light' || theme === 'dark') {
            this.applyTheme(theme);
            this.updateButtonState();
        }
    }
    
    getCurrentTheme() {
        return this.theme;
    }
    
    isSystemThemePreferred() {
        return !this.getStoredTheme();
    }
    
    resetToSystemTheme() {
        try {
            localStorage.removeItem(this.storageKey);
        } catch (e) {
            console.warn('Could not clear theme preference');
        }
        
        const systemTheme = this.getPreferredTheme();
        this.applyTheme(systemTheme);
        this.updateButtonState();
    }
}

/* =============================================================================
   CSS STYLES FOR TOGGLE BUTTON
   ============================================================================= */

// Inject styles for the toggle button
const styleSheet = document.createElement('style');
styleSheet.textContent = `
    /* Theme Toggle Button Styles */
    .btn-theme-toggle {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        border: 2px solid var(--border-primary);
        background-color: var(--bg-surface);
        display: flex;
        align-items: center;
        justify-content: center;
        position: relative;
        overflow: hidden;
        transition: all 0.3s ease;
        cursor: pointer;
    }
    
    .btn-theme-toggle:hover {
        background-color: var(--bg-elevated);
        border-color: var(--color-primary-300);
        transform: scale(1.05);
        box-shadow: var(--shadow-md);
    }
    
    .btn-theme-toggle:focus {
        outline: none;
        box-shadow: 0 0 0 2px var(--color-primary-500);
    }
    
    .theme-toggle-icon {
        position: relative;
        width: 20px;
        height: 20px;
        display: flex;
        align-items: center;
        justify-content: center;
    }
    
    .theme-icon {
        position: absolute;
        font-size: 16px;
        transition: all 0.3s ease;
        transform-origin: center;
    }
    
    .theme-icon-light {
        color: #fbbf24;
        opacity: 1;
        transform: rotate(0deg) scale(1);
    }
    
    .theme-icon-dark {
        color: #60a5fa;
        opacity: 0;
        transform: rotate(180deg) scale(0);
    }
    
    [data-theme="dark"] .theme-icon-light {
        opacity: 0;
        transform: rotate(-180deg) scale(0);
    }
    
    [data-theme="dark"] .theme-icon-dark {
        opacity: 1;
        transform: rotate(0deg) scale(1);
    }
    
    .theme-toggle-animating {
        transform: scale(0.95);
    }
    
    .theme-toggle-animating .theme-icon {
        animation: themeIconBounce 0.3s ease;
    }
    
    @keyframes themeIconBounce {
        0% { transform: scale(1); }
        50% { transform: scale(1.2); }
        100% { transform: scale(1); }
    }
    
    /* Theme Toggle Container */
    .theme-toggle-container {
        position: relative;
    }
    
    .theme-toggle-container::before {
        content: '';
        position: absolute;
        top: 50%;
        left: 50%;
        width: 60px;
        height: 60px;
        border-radius: 50%;
        background: radial-gradient(circle, var(--color-primary-500) 0%, transparent 70%);
        opacity: 0;
        transform: translate(-50%, -50%) scale(0);
        transition: all 0.3s ease;
        z-index: -1;
        pointer-events: none;
    }
    
    .btn-theme-toggle:active + .theme-toggle-container::before,
    .theme-toggle-animating::before {
        opacity: 0.1;
        transform: translate(-50%, -50%) scale(1);
    }
    
    /* Responsive Design */
    @media (max-width: 768px) {
        .btn-theme-toggle {
            width: 36px;
            height: 36px;
        }
        
        .theme-icon {
            font-size: 14px;
        }
    }
`;

document.head.appendChild(styleSheet);

/* =============================================================================
   INITIALIZATION
   ============================================================================= */

// Initialize dark mode when DOM is ready
let darkModeManager;

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        darkModeManager = new DarkModeManager();
    });
} else {
    darkModeManager = new DarkModeManager();
}

// Export for global access
window.DarkModeManager = DarkModeManager;
window.darkMode = () => darkModeManager;

/* =============================================================================
   ACCESSIBILITY ENHANCEMENTS
   ============================================================================= */

// Announce theme changes to screen readers
document.addEventListener('themeChanged', (e) => {
    const announcement = document.createElement('div');
    announcement.setAttribute('aria-live', 'polite');
    announcement.setAttribute('aria-atomic', 'true');
    announcement.className = 'sr-only';
    announcement.textContent = `Theme changed to ${e.detail.theme} mode`;
    
    document.body.appendChild(announcement);
    
    // Remove announcement after it's been read
    setTimeout(() => {
        document.body.removeChild(announcement);
    }, 1000);
});

/* =============================================================================
   DEBUG AND DEVELOPMENT HELPERS
   ============================================================================= */

// Development helper for testing themes
if (process?.env?.NODE_ENV === 'development') {
    window.debugTheme = {
        setLight: () => darkModeManager?.setTheme('light'),
        setDark: () => darkModeManager?.setTheme('dark'),
        toggle: () => darkModeManager?.toggleTheme(),
        reset: () => darkModeManager?.resetToSystemTheme(),
        current: () => darkModeManager?.getCurrentTheme()
    };
    
    console.log('🌙 Dark mode debug helpers available: window.debugTheme');
}