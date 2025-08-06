/* =============================================================================
   MOBILE INTERACTIONS AND TOUCH ENHANCEMENTS
   =============================================================================
   
   Enhanced mobile experience with touch gestures, responsive behaviors,
   and mobile-specific optimizations for the Property Management application
   
   ============================================================================= */

class MobileInteractionManager {
    constructor() {
        this.isMobile = this.detectMobile();
        this.isTouch = 'ontouchstart' in window || navigator.maxTouchPoints > 0;
        this.sidebarOpen = false;
        this.scrollPosition = 0;
        
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
        this.setupMobileSidebar();
        this.setupTouchEnhancements();
        this.setupResponsiveTables();
        this.setupMobileFormOptimizations();
        this.setupSwipeGestures();
        this.setupPullToRefresh();
        this.setupViewportHandling();
        this.setupPerformanceOptimizations();
    }

    /* =========================================================================
       DEVICE DETECTION
       ========================================================================= */
    
    detectMobile() {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
               window.innerWidth <= 768;
    }

    /* =========================================================================
       MOBILE SIDEBAR MANAGEMENT
       ========================================================================= */
    
    setupMobileSidebar() {
        if (!this.isMobile) return;
        
        // Create hamburger menu button
        this.createHamburgerButton();
        
        // Create overlay
        this.createSidebarOverlay();
        
        // Setup event listeners
        this.setupSidebarListeners();
    }
    
    createHamburgerButton() {
        const navbar = document.querySelector('.navbar');
        if (!navbar) return;
        
        const hamburger = document.createElement('button');
        hamburger.className = 'hamburger-btn';
        hamburger.setAttribute('aria-label', 'Toggle navigation menu');
        hamburger.innerHTML = `
            <span class="hamburger-line"></span>
            <span class="hamburger-line"></span>
            <span class="hamburger-line"></span>
        `;
        
        navbar.prepend(hamburger);
        
        // Add styles
        this.injectHamburgerStyles();
        
        hamburger.addEventListener('click', () => this.toggleSidebar());
    }
    
    createSidebarOverlay() {
        const overlay = document.createElement('div');
        overlay.className = 'sidebar-overlay';
        document.body.appendChild(overlay);
        
        overlay.addEventListener('click', () => this.closeSidebar());
    }
    
    setupSidebarListeners() {
        // Close sidebar when clicking nav links on mobile
        const navLinks = document.querySelectorAll('.sidebar .nav-link');
        navLinks.forEach(link => {
            link.addEventListener('click', () => {
                if (this.isMobile) {
                    this.closeSidebar();
                }
            });
        });
        
        // Handle escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.sidebarOpen) {
                this.closeSidebar();
            }
        });
        
        // Handle window resize
        window.addEventListener('resize', () => {
            this.isMobile = this.detectMobile();
            if (!this.isMobile && this.sidebarOpen) {
                this.closeSidebar();
            }
        });
    }
    
    toggleSidebar() {
        if (this.sidebarOpen) {
            this.closeSidebar();
        } else {
            this.openSidebar();
        }
    }
    
    openSidebar() {
        const sidebar = document.querySelector('.sidebar');
        const overlay = document.querySelector('.sidebar-overlay');
        const hamburger = document.querySelector('.hamburger-btn');
        
        if (sidebar && overlay) {
            // Prevent body scroll
            this.scrollPosition = window.pageYOffset;
            document.body.style.overflow = 'hidden';
            document.body.style.position = 'fixed';
            document.body.style.top = `-${this.scrollPosition}px`;
            document.body.style.width = '100%';
            
            sidebar.classList.add('show');
            overlay.classList.add('show');
            hamburger?.classList.add('open');
            
            this.sidebarOpen = true;
            
            // Focus first link for accessibility
            const firstLink = sidebar.querySelector('.nav-link');
            firstLink?.focus();
        }
    }
    
    closeSidebar() {
        const sidebar = document.querySelector('.sidebar');
        const overlay = document.querySelector('.sidebar-overlay');
        const hamburger = document.querySelector('.hamburger-btn');
        
        if (sidebar && overlay) {
            sidebar.classList.remove('show');
            overlay.classList.remove('show');
            hamburger?.classList.remove('open');
            
            // Restore body scroll
            document.body.style.overflow = '';
            document.body.style.position = '';
            document.body.style.top = '';
            document.body.style.width = '';
            window.scrollTo(0, this.scrollPosition);
            
            this.sidebarOpen = false;
        }
    }
    
    injectHamburgerStyles() {
        if (document.getElementById('hamburger-styles')) return;
        
        const styles = document.createElement('style');
        styles.id = 'hamburger-styles';
        styles.textContent = `
            .hamburger-btn {
                position: fixed;
                top: 1rem;
                left: 1rem;
                width: 44px;
                height: 44px;
                background: var(--bg-surface);
                border: 2px solid var(--border-primary);
                border-radius: var(--radius-lg);
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                gap: 4px;
                z-index: 1041;
                transition: all 0.3s ease;
                cursor: pointer;
            }
            
            .hamburger-btn:hover {
                background: var(--bg-elevated);
                border-color: var(--color-primary-300);
                transform: scale(1.05);
            }
            
            .hamburger-line {
                width: 20px;
                height: 2px;
                background: var(--text-primary);
                border-radius: 1px;
                transition: all 0.3s ease;
                transform-origin: center;
            }
            
            .hamburger-btn.open .hamburger-line:nth-child(1) {
                transform: translateY(6px) rotate(45deg);
            }
            
            .hamburger-btn.open .hamburger-line:nth-child(2) {
                opacity: 0;
                transform: scaleX(0);
            }
            
            .hamburger-btn.open .hamburger-line:nth-child(3) {
                transform: translateY(-6px) rotate(-45deg);
            }
            
            @media (min-width: 992px) {
                .hamburger-btn {
                    display: none;
                }
            }
        `;
        
        document.head.appendChild(styles);
    }

    /* =========================================================================
       TOUCH ENHANCEMENTS
       ========================================================================= */
    
    setupTouchEnhancements() {
        if (!this.isTouch) return;
        
        // Add touch feedback to interactive elements
        this.addTouchFeedback();
        
        // Optimize touch targets
        this.optimizeTouchTargets();
        
        // Handle touch events for better responsiveness
        this.setupTouchEvents();
    }
    
    addTouchFeedback() {
        const interactiveElements = document.querySelectorAll('button, .btn, .nav-link, .dropdown-item');
        
        interactiveElements.forEach(element => {
            element.addEventListener('touchstart', (e) => {
                element.classList.add('touch-active');
            }, { passive: true });
            
            element.addEventListener('touchend', (e) => {
                setTimeout(() => {
                    element.classList.remove('touch-active');
                }, 150);
            }, { passive: true });
            
            element.addEventListener('touchcancel', (e) => {
                element.classList.remove('touch-active');
            }, { passive: true });
        });
        
        // Add touch feedback styles
        this.injectTouchStyles();
    }
    
    optimizeTouchTargets() {
        const smallElements = document.querySelectorAll('.btn-sm, .badge, .nav-badge');
        
        smallElements.forEach(element => {
            const rect = element.getBoundingClientRect();
            if (rect.width < 44 || rect.height < 44) {
                element.style.minWidth = '44px';
                element.style.minHeight = '44px';
                element.style.display = 'inline-flex';
                element.style.alignItems = 'center';
                element.style.justifyContent = 'center';
            }
        });
    }
    
    setupTouchEvents() {
        // Prevent double-tap zoom on buttons
        const buttons = document.querySelectorAll('button, .btn');
        buttons.forEach(button => {
            button.addEventListener('touchend', (e) => {
                e.preventDefault();
                button.click();
            });
        });
    }
    
    injectTouchStyles() {
        if (document.getElementById('touch-styles')) return;
        
        const styles = document.createElement('style');
        styles.id = 'touch-styles';
        styles.textContent = `
            .touch-active {
                background-color: rgba(59, 130, 246, 0.1) !important;
                transform: scale(0.98) !important;
                transition: all 0.1s ease !important;
            }
            
            /* Disable hover effects on touch devices */
            @media (hover: none) and (pointer: coarse) {
                .btn:hover,
                .nav-link:hover,
                .card:hover {
                    transform: none !important;
                    box-shadow: inherit !important;
                }
                
                .btn:active,
                .nav-link:active {
                    transform: scale(0.98) !important;
                    background-color: rgba(59, 130, 246, 0.1) !important;
                }
            }
        `;
        
        document.head.appendChild(styles);
    }

    /* =========================================================================
       RESPONSIVE TABLES
       ========================================================================= */
    
    setupResponsiveTables() {
        if (!this.isMobile) return;
        
        const tables = document.querySelectorAll('.table');
        
        tables.forEach(table => {
            this.convertToMobileCardView(table);
        });
    }
    
    convertToMobileCardView(table) {
        const headers = Array.from(table.querySelectorAll('thead th')).map(th => th.textContent.trim());
        const rows = table.querySelectorAll('tbody tr');
        
        table.classList.add('mobile-card-table');
        
        rows.forEach(row => {
            const cells = row.querySelectorAll('td');
            cells.forEach((cell, index) => {
                if (headers[index]) {
                    cell.setAttribute('data-label', headers[index]);
                }
            });
        });
    }

    /* =========================================================================
       MOBILE FORM OPTIMIZATIONS
       ========================================================================= */
    
    setupMobileFormOptimizations() {
        if (!this.isMobile) return;
        
        // Prevent iOS zoom on input focus
        this.preventIOSZoom();
        
        // Optimize form modals for mobile
        this.optimizeFormModals();
        
        // Add mobile-friendly validation
        this.setupMobileValidation();
    }
    
    preventIOSZoom() {
        const inputs = document.querySelectorAll('input, select, textarea');
        
        inputs.forEach(input => {
            if (input.style.fontSize !== '16px') {
                input.style.fontSize = '16px';
            }
        });
    }
    
    optimizeFormModals() {
        const modals = document.querySelectorAll('.modal');
        
        modals.forEach(modal => {
            const dialog = modal.querySelector('.modal-dialog');
            if (dialog && this.isMobile) {
                dialog.style.margin = '0.5rem';
                dialog.style.maxWidth = 'calc(100% - 1rem)';
                dialog.style.width = 'calc(100% - 1rem)';
            }
        });
        
        // Handle virtual keyboard
        this.handleVirtualKeyboard();
    }
    
    handleVirtualKeyboard() {
        const originalViewportHeight = window.innerHeight;
        
        window.addEventListener('resize', () => {
            const currentViewportHeight = window.innerHeight;
            const heightDifference = originalViewportHeight - currentViewportHeight;
            
            if (heightDifference > 150) {
                // Virtual keyboard is likely open
                document.body.classList.add('keyboard-open');
            } else {
                document.body.classList.remove('keyboard-open');
            }
        });
        
        // Add styles for keyboard handling
        this.injectKeyboardStyles();
    }
    
    setupMobileValidation() {
        const forms = document.querySelectorAll('form');
        
        forms.forEach(form => {
            form.addEventListener('submit', (e) => {
                // Add visual feedback for form submission on mobile
                const submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
                if (submitBtn) {
                    submitBtn.innerHTML = '<span class="loading-spinner"></span> Processing...';
                    submitBtn.disabled = true;
                }
            });
        });
    }
    
    injectKeyboardStyles() {
        if (document.getElementById('keyboard-styles')) return;
        
        const styles = document.createElement('style');
        styles.id = 'keyboard-styles';
        styles.textContent = `
            .keyboard-open .modal-dialog {
                transform: translateY(-25%) !important;
            }
            
            .keyboard-open .fixed-bottom {
                display: none !important;
            }
        `;
        
        document.head.appendChild(styles);
    }

    /* =========================================================================
       SWIPE GESTURES
       ========================================================================= */
    
    setupSwipeGestures() {
        if (!this.isTouch) return;
        
        // Setup swipe for sidebar
        this.setupSidebarSwipe();
        
        // Setup swipe for tabs
        this.setupTabSwipe();
    }
    
    setupSidebarSwipe() {
        let startX = 0;
        let startY = 0;
        let isScrolling = null;
        
        document.addEventListener('touchstart', (e) => {
            startX = e.touches[0].pageX;
            startY = e.touches[0].pageY;
        }, { passive: true });
        
        document.addEventListener('touchmove', (e) => {
            if (e.touches.length > 1) return;
            
            const deltaX = e.touches[0].pageX - startX;
            const deltaY = e.touches[0].pageY - startY;
            
            if (isScrolling === null) {
                isScrolling = Math.abs(deltaX) < Math.abs(deltaY);
            }
            
            if (isScrolling) return;
            
            // Swipe right from left edge to open sidebar
            if (startX < 20 && deltaX > 50 && !this.sidebarOpen) {
                this.openSidebar();
            }
            
            // Swipe left to close sidebar
            if (this.sidebarOpen && deltaX < -50) {
                this.closeSidebar();
            }
        }, { passive: true });
        
        document.addEventListener('touchend', () => {
            isScrolling = null;
        }, { passive: true });
    }
    
    setupTabSwipe() {
        const tabContents = document.querySelectorAll('.tab-content');
        
        tabContents.forEach(content => {
            let startX = 0;
            let isScrolling = null;
            
            content.addEventListener('touchstart', (e) => {
                startX = e.touches[0].pageX;
            }, { passive: true });
            
            content.addEventListener('touchmove', (e) => {
                const deltaX = e.touches[0].pageX - startX;
                
                if (isScrolling === null) {
                    isScrolling = Math.abs(deltaX) < 30;
                }
                
                if (isScrolling) return;
                
                e.preventDefault();
            }, { passive: false });
            
            content.addEventListener('touchend', (e) => {
                const deltaX = e.changedTouches[0].pageX - startX;
                
                if (Math.abs(deltaX) > 50 && !isScrolling) {
                    this.switchTab(content, deltaX > 0 ? 'prev' : 'next');
                }
                
                isScrolling = null;
            }, { passive: true });
        });
    }
    
    switchTab(content, direction) {
        const activeTab = content.querySelector('.tab-pane.active');
        if (!activeTab) return;
        
        const allTabs = Array.from(content.querySelectorAll('.tab-pane'));
        const currentIndex = allTabs.indexOf(activeTab);
        
        let nextIndex;
        if (direction === 'next') {
            nextIndex = currentIndex + 1 < allTabs.length ? currentIndex + 1 : 0;
        } else {
            nextIndex = currentIndex - 1 >= 0 ? currentIndex - 1 : allTabs.length - 1;
        }
        
        // Trigger tab change
        const nextTabId = allTabs[nextIndex].id;
        const tabButton = document.querySelector(`[data-bs-target="#${nextTabId}"]`);
        if (tabButton) {
            tabButton.click();
        }
    }

    /* =========================================================================
       PULL TO REFRESH
       ========================================================================= */
    
    setupPullToRefresh() {
        if (!this.isTouch) return;
        
        let startY = 0;
        let pullDistance = 0;
        let isPulling = false;
        const pullThreshold = 80;
        
        const content = document.querySelector('.main-content');
        if (!content) return;
        
        content.classList.add('pull-to-refresh');
        
        content.addEventListener('touchstart', (e) => {
            startY = e.touches[0].pageY;
        }, { passive: true });
        
        content.addEventListener('touchmove', (e) => {
            if (window.pageYOffset !== 0) return;
            
            pullDistance = e.touches[0].pageY - startY;
            
            if (pullDistance > 0 && pullDistance < pullThreshold * 2) {
                isPulling = true;
                content.style.transform = `translateY(${pullDistance / 3}px)`;
                
                if (pullDistance > pullThreshold) {
                    content.classList.add('pulling');
                }
            }
        }, { passive: true });
        
        content.addEventListener('touchend', (e) => {
            if (isPulling) {
                content.style.transform = '';
                
                if (pullDistance > pullThreshold) {
                    this.triggerRefresh();
                }
                
                content.classList.remove('pulling');
                isPulling = false;
                pullDistance = 0;
            }
        }, { passive: true });
    }
    
    triggerRefresh() {
        // Show loading indicator
        if (window.toastr) {
            window.toastr.info('Refreshing...', '', {
                timeOut: 1000,
                showDuration: 300
            });
        }
        
        // Refresh the page after a short delay
        setTimeout(() => {
            window.location.reload();
        }, 1000);
    }

    /* =========================================================================
       VIEWPORT HANDLING
       ========================================================================= */
    
    setupViewportHandling() {
        // Handle viewport height changes (iOS Safari)
        this.setViewportHeight();
        
        window.addEventListener('resize', () => {
            this.setViewportHeight();
        });
        
        window.addEventListener('orientationchange', () => {
            setTimeout(() => {
                this.setViewportHeight();
            }, 500);
        });
    }
    
    setViewportHeight() {
        const vh = window.innerHeight * 0.01;
        document.documentElement.style.setProperty('--vh', `${vh}px`);
    }

    /* =========================================================================
       PERFORMANCE OPTIMIZATIONS
       ========================================================================= */
    
    setupPerformanceOptimizations() {
        if (!this.isMobile) return;
        
        // Lazy load images
        this.setupLazyLoading();
        
        // Optimize scrolling performance
        this.optimizeScrolling();
        
        // Reduce unnecessary repaints
        this.optimizeRepaints();
    }
    
    setupLazyLoading() {
        if ('IntersectionObserver' in window) {
            const images = document.querySelectorAll('img[data-src]');
            const imageObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        img.src = img.dataset.src;
                        img.removeAttribute('data-src');
                        imageObserver.unobserve(img);
                    }
                });
            });
            
            images.forEach(img => imageObserver.observe(img));
        }
    }
    
    optimizeScrolling() {
        // Use passive event listeners for scroll events
        let scrollTimer = null;
        
        window.addEventListener('scroll', () => {
            if (scrollTimer) {
                clearTimeout(scrollTimer);
            }
            
            scrollTimer = setTimeout(() => {
                this.handleScrollEnd();
            }, 150);
        }, { passive: true });
    }
    
    handleScrollEnd() {
        // Trigger any scroll-dependent updates
        const event = new CustomEvent('scrollEnd');
        document.dispatchEvent(event);
    }
    
    optimizeRepaints() {
        // Use transform instead of changing position
        const animatedElements = document.querySelectorAll('.card, .btn, .nav-link');
        
        animatedElements.forEach(element => {
            element.style.willChange = 'transform';
        });
    }

    /* =========================================================================
       PUBLIC API
       ========================================================================= */
    
    isMobileDevice() {
        return this.isMobile;
    }
    
    isTouchDevice() {
        return this.isTouch;
    }
    
    getSidebarState() {
        return this.sidebarOpen;
    }
    
    refreshMobileOptimizations() {
        this.isMobile = this.detectMobile();
        if (this.isMobile) {
            this.setupResponsiveTables();
            this.setupMobileFormOptimizations();
        }
    }
}

/* =============================================================================
   INITIALIZATION
   ============================================================================= */

let mobileManager;

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        mobileManager = new MobileInteractionManager();
    });
} else {
    mobileManager = new MobileInteractionManager();
}

// Export for global access
window.MobileInteractionManager = MobileInteractionManager;
window.mobileManager = () => mobileManager;