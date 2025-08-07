/* =============================================================================
   PERFORMANCE OPTIMIZATION SYSTEM
   =============================================================================
   
   Comprehensive performance enhancements including lazy loading, resource
   optimization, bundle management, and runtime performance monitoring
   
   ============================================================================= */

class PerformanceOptimizer {
    constructor() {
        this.isInitialized = false;
        this.lazyImages = [];
        this.deferredScripts = [];
        this.performanceMetrics = {};
        this.observers = {};
        
        this.init();
    }

    /* =========================================================================
       INITIALIZATION
       ========================================================================= */
    
    init() {
        if (this.isInitialized) return;
        
        // Start performance measurement
        this.startPerformanceMeasurement();
        
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.setupAfterDOMReady());
        } else {
            this.setupAfterDOMReady();
        }
    }

    setupAfterDOMReady() {
        this.setupLazyLoading();
        this.setupResourceOptimization();
        this.setupCriticalResourceHints();
        this.setupServiceWorker();
        this.optimizeRendering();
        this.setupPerformanceMonitoring();
        this.optimizeDataTables();
        this.optimizeFormSubmissions();
        this.setupConnectionOptimization();
        this.isInitialized = true;
        
        this.measureInitializationTime();
    }

    /* =========================================================================
       PERFORMANCE MEASUREMENT
       ========================================================================= */
    
    startPerformanceMeasurement() {
        if (!performance.mark) return;
        
        performance.mark('app-initialization-start');
        
        // Measure key performance metrics
        window.addEventListener('load', () => {
            this.collectPerformanceMetrics();
        });
    }
    
    collectPerformanceMetrics() {
        if (!performance.getEntriesByType) return;
        
        const navigation = performance.getEntriesByType('navigation')[0];
        const paint = performance.getEntriesByType('paint');
        
        this.performanceMetrics = {
            domContentLoaded: navigation?.domContentLoadedEventEnd - navigation?.domContentLoadedEventStart,
            loadComplete: navigation?.loadEventEnd - navigation?.loadEventStart,
            firstPaint: paint.find(p => p.name === 'first-paint')?.startTime,
            firstContentfulPaint: paint.find(p => p.name === 'first-contentful-paint')?.startTime,
            ttfb: navigation?.responseStart - navigation?.requestStart,
            domInteractive: navigation?.domInteractive - navigation?.navigationStart,
            resourceCount: performance.getEntriesByType('resource').length
        };
        
        // Log performance metrics in development
        if (this.isDevelopment()) {
            console.table(this.performanceMetrics);
        }
        
        // Send metrics to monitoring service (placeholder)
        this.sendMetricsToMonitoring();
    }
    
    measureInitializationTime() {
        if (performance.mark && performance.measure) {
            performance.mark('app-initialization-end');
            performance.measure('app-initialization', 'app-initialization-start', 'app-initialization-end');
            
            const measure = performance.getEntriesByName('app-initialization')[0];
            this.performanceMetrics.initializationTime = measure?.duration;
        }
    }

    /* =========================================================================
       LAZY LOADING
       ========================================================================= */
    
    setupLazyLoading() {
        this.lazyLoadImages();
        this.lazyLoadComponents();
        this.lazyLoadScripts();
    }
    
    lazyLoadImages() {
        if (!('IntersectionObserver' in window)) {
            this.loadAllImages();
            return;
        }
        
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    this.loadImage(entry.target);
                    imageObserver.unobserve(entry.target);
                }
            });
        }, {
            rootMargin: '50px 0px',
            threshold: 0.1
        });
        
        // Find images with data-src attribute
        const lazyImages = document.querySelectorAll('img[data-src]');
        lazyImages.forEach(img => {
            imageObserver.observe(img);
        });
        
        this.observers.images = imageObserver;
    }
    
    loadImage(img) {
        const src = img.dataset.src;
        if (!src) return;
        
        img.src = src;
        img.removeAttribute('data-src');
        
        img.onload = () => {
            img.classList.add('loaded');
        };
        
        img.onerror = () => {
            img.classList.add('error');
        };
    }
    
    loadAllImages() {
        const lazyImages = document.querySelectorAll('img[data-src]');
        lazyImages.forEach(img => this.loadImage(img));
    }
    
    lazyLoadComponents() {
        if (!('IntersectionObserver' in window)) return;
        
        const componentObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    this.loadComponent(entry.target);
                    componentObserver.unobserve(entry.target);
                }
            });
        }, {
            rootMargin: '100px 0px'
        });
        
        const lazyComponents = document.querySelectorAll('[data-lazy-component]');
        lazyComponents.forEach(component => {
            componentObserver.observe(component);
        });
        
        this.observers.components = componentObserver;
    }
    
    loadComponent(element) {
        const componentName = element.dataset.lazyComponent;
        if (!componentName) return;
        
        // Add loading state
        element.classList.add('loading');
        
        // Load component-specific resources
        this.loadComponentResources(componentName).then(() => {
            element.classList.remove('loading');
            element.classList.add('loaded');
        });
    }
    
    loadComponentResources(componentName) {
        return Promise.all([
            this.loadComponentCSS(componentName),
            this.loadComponentJS(componentName)
        ]);
    }
    
    loadComponentCSS(componentName) {
        return new Promise((resolve) => {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = `/css/components/${componentName}.css`;
            link.onload = resolve;
            link.onerror = resolve; // Don't fail if CSS doesn't exist
            document.head.appendChild(link);
        });
    }
    
    loadComponentJS(componentName) {
        return new Promise((resolve) => {
            const script = document.createElement('script');
            script.src = `/js/components/${componentName}.js`;
            script.onload = resolve;
            script.onerror = resolve; // Don't fail if JS doesn't exist
            document.head.appendChild(script);
        });
    }
    
    lazyLoadScripts() {
        const deferredScripts = document.querySelectorAll('script[data-defer]');
        
        deferredScripts.forEach(script => {
            const newScript = document.createElement('script');
            
            if (script.src) {
                newScript.src = script.src;
            } else {
                newScript.textContent = script.textContent;
            }
            
            // Load script after a delay or on user interaction
            const delay = parseInt(script.dataset.defer) || 1000;
            
            if (delay > 0) {
                setTimeout(() => {
                    document.head.appendChild(newScript);
                }, delay);
            } else {
                // Load on first user interaction
                this.loadOnInteraction(newScript);
            }
            
            script.remove();
        });
    }
    
    loadOnInteraction(script) {
        const events = ['mousedown', 'touchstart', 'keydown', 'scroll'];
        
        const loadScript = () => {
            document.head.appendChild(script);
            events.forEach(event => {
                document.removeEventListener(event, loadScript, { passive: true });
            });
        };
        
        events.forEach(event => {
            document.addEventListener(event, loadScript, { passive: true });
        });
    }

    /* =========================================================================
       RESOURCE OPTIMIZATION
       ========================================================================= */
    
    setupResourceOptimization() {
        this.prefetchCriticalResources();
        this.optimizeImages();
        this.optimizeFonts();
        this.removeUnusedCSS();
    }
    
    prefetchCriticalResources() {
        const criticalPaths = [
            '/Tenants',
            '/Rooms',
            '/Payments'
        ];
        
        criticalPaths.forEach(path => {
            const link = document.createElement('link');
            link.rel = 'prefetch';
            link.href = path;
            document.head.appendChild(link);
        });
    }
    
    optimizeImages() {
        // Convert images to WebP format if supported
        if (this.supportsWebP()) {
            const images = document.querySelectorAll('img');
            images.forEach(img => {
                if (img.src && !img.src.includes('.webp')) {
                    const webpSrc = img.src.replace(/\.(jpg|jpeg|png)$/i, '.webp');
                    
                    // Test if WebP version exists
                    this.imageExists(webpSrc).then(exists => {
                        if (exists) {
                            img.src = webpSrc;
                        }
                    });
                }
            });
        }
    }
    
    supportsWebP() {
        const canvas = document.createElement('canvas');
        canvas.width = canvas.height = 1;
        return canvas.toDataURL('image/webp').indexOf('data:image/webp') === 0;
    }
    
    imageExists(src) {
        return new Promise((resolve) => {
            const img = new Image();
            img.onload = () => resolve(true);
            img.onerror = () => resolve(false);
            img.src = src;
        });
    }
    
    optimizeFonts() {
        // Font optimization disabled - using Google Fonts instead
        // Local font files would be preloaded here if available
        console.log('Font optimization: Using Google Fonts, no local preloading needed');
    }
    
    removeUnusedCSS() {
        // This is a placeholder for more advanced unused CSS removal
        // In a real application, you would use tools like PurgeCSS
        console.log('CSS optimization would be handled by build tools');
    }

    /* =========================================================================
       CRITICAL RESOURCE HINTS
       ========================================================================= */
    
    setupCriticalResourceHints() {
        this.addDNSPrefetch();
        this.addPreconnect();
        this.addResourceHints();
    }
    
    addDNSPrefetch() {
        const externalDomains = [
            '//cdnjs.cloudflare.com',
            '//fonts.googleapis.com',
            '//fonts.gstatic.com'
        ];
        
        externalDomains.forEach(domain => {
            const link = document.createElement('link');
            link.rel = 'dns-prefetch';
            link.href = domain;
            document.head.appendChild(link);
        });
    }
    
    addPreconnect() {
        const criticalDomains = [
            'https://fonts.googleapis.com',
            'https://fonts.gstatic.com'
        ];
        
        criticalDomains.forEach(domain => {
            const link = document.createElement('link');
            link.rel = 'preconnect';
            link.href = domain;
            link.crossOrigin = 'anonymous';
            document.head.appendChild(link);
        });
    }
    
    addResourceHints() {
        // Preload critical CSS
        const criticalCSS = [
            '/css/design-system.css',
            '/css/components.css'
        ];
        
        criticalCSS.forEach(href => {
            const link = document.createElement('link');
            link.rel = 'preload';
            link.as = 'style';
            link.href = href;
            link.onload = () => {
                link.onload = null;
                link.rel = 'stylesheet';
            };
            document.head.appendChild(link);
        });
    }

    /* =========================================================================
       SERVICE WORKER
       ========================================================================= */
    
    setupServiceWorker() {
        // Service worker disabled - no sw.js file available
        // Would register service worker for offline support if implemented
        console.log('Service worker: Not implemented, skipping registration');
    }

    /* =========================================================================
       RENDERING OPTIMIZATION
       ========================================================================= */
    
    optimizeRendering() {
        this.optimizeScrollPerformance();
        this.optimizeAnimations();
        this.optimizeRepaints();
        this.setupVirtualScrolling();
    }
    
    optimizeScrollPerformance() {
        let ticking = false;
        
        const optimizedScroll = () => {
            // Batched scroll operations
            this.updateScrollDependentElements();
            ticking = false;
        };
        
        document.addEventListener('scroll', () => {
            if (!ticking) {
                requestAnimationFrame(optimizedScroll);
                ticking = true;
            }
        }, { passive: true });
    }
    
    updateScrollDependentElements() {
        const scrollY = window.scrollY;
        
        // Update elements that depend on scroll position
        const parallaxElements = document.querySelectorAll('[data-parallax]');
        parallaxElements.forEach(element => {
            const speed = parseFloat(element.dataset.parallax) || 0.5;
            const yPos = -(scrollY * speed);
            element.style.transform = `translateY(${yPos}px)`;
        });
    }
    
    optimizeAnimations() {
        // Use CSS containment for better performance
        const animatedElements = document.querySelectorAll('.btn, .card, .nav-link');
        animatedElements.forEach(element => {
            element.style.contain = 'layout style paint';
        });
        
        // Prefer transform over position changes
        const movingElements = document.querySelectorAll('[data-animate]');
        movingElements.forEach(element => {
            element.style.willChange = 'transform';
        });
    }
    
    optimizeRepaints() {
        // Use transform for hover effects instead of position
        const hoverElements = document.querySelectorAll('.btn, .card');
        hoverElements.forEach(element => {
            element.addEventListener('mouseenter', () => {
                element.style.willChange = 'transform';
            });
            
            element.addEventListener('mouseleave', () => {
                element.style.willChange = 'auto';
            });
        });
    }
    
    setupVirtualScrolling() {
        const longLists = document.querySelectorAll('.table tbody, .long-list');
        
        longLists.forEach(list => {
            if (list.children.length > 100) {
                this.virtualizeList(list);
            }
        });
    }
    
    virtualizeList(list) {
        // Simple virtual scrolling implementation
        const items = Array.from(list.children);
        const itemHeight = items[0]?.offsetHeight || 50;
        const visibleItems = Math.ceil(window.innerHeight / itemHeight) + 5;
        
        let startIndex = 0;
        
        const updateVisibleItems = () => {
            const scrollTop = list.scrollTop || window.scrollY;
            startIndex = Math.floor(scrollTop / itemHeight);
            const endIndex = Math.min(startIndex + visibleItems, items.length);
            
            // Hide all items
            items.forEach((item, index) => {
                if (index < startIndex || index > endIndex) {
                    item.style.display = 'none';
                } else {
                    item.style.display = '';
                }
            });
        };
        
        // Throttled scroll handler
        let scrollTimeout;
        const handleScroll = () => {
            if (scrollTimeout) clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(updateVisibleItems, 10);
        };
        
        window.addEventListener('scroll', handleScroll, { passive: true });
        updateVisibleItems();
    }

    /* =========================================================================
       PERFORMANCE MONITORING
       ========================================================================= */
    
    setupPerformanceMonitoring() {
        this.monitorMemoryUsage();
        this.monitorNetworkRequests();
        this.setupErrorTracking();
    }
    
    monitorMemoryUsage() {
        if (!performance.memory) return;
        
        setInterval(() => {
            const memory = performance.memory;
            const usage = {
                used: memory.usedJSHeapSize / 1048576, // MB
                total: memory.totalJSHeapSize / 1048576, // MB
                limit: memory.jsHeapSizeLimit / 1048576 // MB
            };
            
            if (usage.used > usage.limit * 0.9) {
                console.warn('Memory usage approaching limit', usage);
                this.cleanupMemory();
            }
        }, 30000); // Check every 30 seconds
    }
    
    cleanupMemory() {
        // Remove event listeners from hidden elements
        const hiddenElements = document.querySelectorAll('[style*="display: none"]');
        hiddenElements.forEach(element => {
            element.replaceWith(element.cloneNode(true));
        });
        
        // Clear caches
        if (this.observers.images) {
            this.observers.images.disconnect();
        }
        if (this.observers.components) {
            this.observers.components.disconnect();
        }
        
        // Force garbage collection if available
        if (window.gc) {
            window.gc();
        }
    }
    
    monitorNetworkRequests() {
        if (!performance.getEntriesByType) return;
        
        const observer = new PerformanceObserver((list) => {
            list.getEntries().forEach(entry => {
                if (entry.duration > 1000) { // Slow request > 1s
                    console.warn('Slow network request detected:', {
                        url: entry.name,
                        duration: entry.duration
                    });
                }
            });
        });
        
        observer.observe({ entryTypes: ['resource'] });
    }
    
    setupErrorTracking() {
        window.addEventListener('error', (event) => {
            this.logError({
                message: event.message,
                source: event.filename,
                line: event.lineno,
                column: event.colno,
                error: event.error
            });
        });
        
        window.addEventListener('unhandledrejection', (event) => {
            this.logError({
                message: 'Unhandled Promise Rejection',
                reason: event.reason
            });
        });
    }
    
    logError(errorInfo) {
        // Log error to console in development
        if (this.isDevelopment()) {
            console.error('Application Error:', errorInfo);
        }
        
        // Send to monitoring service in production
        this.sendErrorToMonitoring(errorInfo);
    }

    /* =========================================================================
       DATATABLE OPTIMIZATION
       ========================================================================= */
    
    optimizeDataTables() {
        if (!window.$ || !window.$.fn.DataTable) return;
        
        // Don't override DataTables pagination renderer - it breaks button text
        // Just optimize existing DataTables for performance
        window.$('.datatable, [data-datatable]').each(function() {
            const table = window.$(this);
            
            if (table.hasClass('dataTable')) {
                const api = table.DataTable();
                
                // Enable deferred rendering for large datasets
                if (api.data().count() > 100) {
                    api.settings()[0].deferRender = true;
                    api.draw();
                }
            }
        });
    }

    /* =========================================================================
       FORM OPTIMIZATION
       ========================================================================= */
    
    optimizeFormSubmissions() {
        const forms = document.querySelectorAll('form');
        
        forms.forEach(form => {
            this.optimizeForm(form);
        });
    }
    
    optimizeForm(form) {
        // Debounce form validation
        const inputs = form.querySelectorAll('input, select, textarea');
        
        inputs.forEach(input => {
            let timeout;
            input.addEventListener('input', () => {
                clearTimeout(timeout);
                timeout = setTimeout(() => {
                    this.validateField(input);
                }, 300);
            });
        });
        
        // Prevent double submissions
        form.addEventListener('submit', (e) => {
            if (form.dataset.submitting === 'true') {
                e.preventDefault();
                return;
            }
            
            form.dataset.submitting = 'true';
            
            // Re-enable form after 5 seconds (fallback)
            setTimeout(() => {
                form.dataset.submitting = 'false';
            }, 5000);
        });
    }
    
    validateField(field) {
        // Asynchronous validation to avoid blocking UI
        requestIdleCallback(() => {
            field.checkValidity();
        });
    }

    /* =========================================================================
       CONNECTION OPTIMIZATION
       ========================================================================= */
    
    setupConnectionOptimization() {
        if ('connection' in navigator) {
            const connection = navigator.connection;
            this.adaptToConnection(connection);
            
            connection.addEventListener('change', () => {
                this.adaptToConnection(connection);
            });
        }
    }
    
    adaptToConnection(connection) {
        const effectiveType = connection.effectiveType;
        
        switch (effectiveType) {
            case 'slow-2g':
            case '2g':
                this.enableDataSavingMode();
                break;
            case '3g':
                this.enableLimitedDataMode();
                break;
            case '4g':
            default:
                this.enableFullFeaturesMode();
                break;
        }
    }
    
    enableDataSavingMode() {
        // Disable non-essential features for slow connections
        document.body.classList.add('data-saving-mode');
        
        // Disable animations
        document.body.style.setProperty('--duration-fast', '0ms');
        document.body.style.setProperty('--duration-normal', '0ms');
        document.body.style.setProperty('--duration-slow', '0ms');
        
        // Load images on demand only
        const images = document.querySelectorAll('img:not([data-src])');
        images.forEach(img => {
            if (img.src) {
                img.dataset.src = img.src;
                img.src = '';
            }
        });
    }
    
    enableLimitedDataMode() {
        document.body.classList.add('limited-data-mode');
        
        // Reduce animation duration
        document.body.style.setProperty('--duration-fast', '50ms');
        document.body.style.setProperty('--duration-normal', '100ms');
        document.body.style.setProperty('--duration-slow', '150ms');
    }
    
    enableFullFeaturesMode() {
        document.body.classList.remove('data-saving-mode', 'limited-data-mode');
        
        // Reset animation durations
        document.body.style.removeProperty('--duration-fast');
        document.body.style.removeProperty('--duration-normal');
        document.body.style.removeProperty('--duration-slow');
    }

    /* =========================================================================
       UTILITIES
       ========================================================================= */
    
    isDevelopment() {
        return window.location.hostname === 'localhost' || 
               window.location.hostname === '127.0.0.1' ||
               window.location.hostname.includes('dev');
    }
    
    sendMetricsToMonitoring() {
        // Placeholder for sending metrics to monitoring service
        if (this.isDevelopment()) {
            console.log('Performance metrics:', this.performanceMetrics);
        }
        
        // In production, you would send to services like:
        // - Google Analytics
        // - Application Insights
        // - DataDog
        // - New Relic
    }
    
    sendErrorToMonitoring(errorInfo) {
        // Placeholder for error monitoring service
        if (!this.isDevelopment()) {
            // Send to error monitoring service
            console.log('Would send error to monitoring:', errorInfo);
        }
    }

    /* =========================================================================
       PUBLIC API
       ========================================================================= */
    
    getPerformanceMetrics() {
        return { ...this.performanceMetrics };
    }
    
    optimizeNow() {
        this.cleanupMemory();
        this.optimizeDataTables();
    }
    
    enableDataSaver() {
        this.enableDataSavingMode();
    }
    
    disableDataSaver() {
        this.enableFullFeaturesMode();
    }
}

/* =============================================================================
   INITIALIZATION
   ============================================================================= */

let performanceOptimizer;

// Initialize immediately for performance benefits
performanceOptimizer = new PerformanceOptimizer();

// Export for global access
window.PerformanceOptimizer = PerformanceOptimizer;
window.performance_optimizer = () => performanceOptimizer;