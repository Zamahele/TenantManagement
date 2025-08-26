using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;
using PropertyManagement.Infrastructure.Data;
using System.Diagnostics;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = "Manager")]
public class HomeController : Controller
{
  private readonly IGenericRepository<Room> _roomRepository;
  private readonly IGenericRepository<Tenant> _tenantRepository;
  private readonly IGenericRepository<LeaseAgreement> _leaseAgreementRepository;
  private readonly IGenericRepository<MaintenanceRequest> _maintenanceRequestRepository;
  private readonly IGenericRepository<Payment> _paymentRepository;
  private readonly IGenericRepository<Inspection> _inspectionRepository;
  private readonly IGenericRepository<BookingRequest> _bookingRequestRepository;
  private readonly ApplicationDbContext _context;
  private readonly ILogger<HomeController> _logger;
  private readonly IMapper _mapper;

  public HomeController(
      IGenericRepository<Room> roomRepository,
      IGenericRepository<Tenant> tenantRepository,
      IGenericRepository<LeaseAgreement> leaseAgreementRepository,
      IGenericRepository<MaintenanceRequest> maintenanceRequestRepository,
      IGenericRepository<Payment> paymentRepository,
      IGenericRepository<Inspection> inspectionRepository,
      IGenericRepository<BookingRequest> bookingRequestRepository,
      ApplicationDbContext context,
      ILogger<HomeController> logger,
      IMapper mapper)
  {
    _roomRepository = roomRepository;
    _tenantRepository = tenantRepository;
    _leaseAgreementRepository = leaseAgreementRepository;
    _maintenanceRequestRepository = maintenanceRequestRepository;
    _paymentRepository = paymentRepository;
    _inspectionRepository = inspectionRepository;
    _bookingRequestRepository = bookingRequestRepository;
    _context = context;
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
  }

  // Add a static counter for home page visits
  private static readonly Counter HomePageVisitCounter =
      Metrics.CreateCounter("home_page_visits_total", "Total number of visits to the home page.");

  // Kubernetes Health Check Endpoints
  [AllowAnonymous]
  [HttpGet("/health")]
  public IActionResult Health()
  {
    return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
  }

  [AllowAnonymous]
  [HttpGet("/health/ready")]
  public async Task<IActionResult> Ready()
  {
    try
    {
      // Check database connectivity
      await _context.Database.CanConnectAsync();
      
      return Ok(new { 
        status = "ready", 
        timestamp = DateTime.UtcNow,
        database = "connected"
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Readiness check failed");
      return StatusCode(503, new { 
        status = "not ready", 
        timestamp = DateTime.UtcNow,
        error = ex.Message 
      });
    }
  }

  [AllowAnonymous]
  [HttpGet("/health/live")]
  public IActionResult Live()
  {
    return Ok(new { 
      status = "alive", 
      timestamp = DateTime.UtcNow,
      uptime = Environment.TickCount64
    });
  }

  public async Task<IActionResult> Index()
  {
    // Increment the Prometheus counter
    HomePageVisitCounter.Inc();

    _logger.LogInformation("Accessing the dashboard at {Time}", DateTime.UtcNow);
    var now = DateTime.Now;
    var today = now.Date;
    var weekStart = today.AddDays(-(int)today.DayOfWeek);
    var weekEnd = weekStart.AddDays(7);
    
    // Fetch all data sequentially to avoid DbContext threading issues
    var rooms = await _roomRepository.GetAllAsync();
    var tenants = await _tenantRepository.GetAllAsync();
    var leases = await _leaseAgreementRepository.GetAllAsync();
    var maintenanceRequests = await _maintenanceRequestRepository.GetAllAsync();
    var payments = await _paymentRepository.GetAllAsync();
    var inspections = await _inspectionRepository.GetAllAsync();
    var bookingRequests = await _bookingRequestRepository.GetAllAsync();

    // Calculate basic property metrics
    var totalRooms = rooms.Count();
    var availableRooms = rooms.Count(r => r.Status == "Available");
    var occupiedRooms = rooms.Count(r => r.Status == "Occupied");
    var underMaintenanceRooms = rooms.Count(r => r.Status == "Under Maintenance");
    
    // Set sidebar counts
    ViewBag.TenantCount = tenants.Count();
    ViewBag.RoomCount = totalRooms;
    ViewBag.PendingMaintenanceCount = maintenanceRequests.Count(m => 
      m.Status == "Pending" || m.Status == "In Progress");
    
    // Calculate financial metrics
    var activeLeases = leases.Where(l => l.StartDate <= now && l.EndDate >= now).ToList();
    var totalMonthlyRent = activeLeases.Sum(l => l.RentAmount);
    
    var currentMonthPayments = payments.Where(p => 
        p.Date.Year == now.Year && 
        p.Date.Month == now.Month && 
        p.Type == "Rent").ToList();
    var collectedRent = currentMonthPayments.Sum(p => p.Amount);
    var outstandingRent = totalMonthlyRent - collectedRent;
    
    // Calculate operational metrics
    var maintenanceRequestsToday = maintenanceRequests.Count(r => r.RequestDate.Date == today);
    var inspectionsDueThisWeek = inspections.Count(i => i.Date.Date >= weekStart && i.Date.Date < weekEnd);
    var newBookingsThisWeek = bookingRequests.Count(b => b.RequestDate.Date >= weekStart && b.RequestDate.Date < weekEnd);
    var pendingRequests = maintenanceRequests.Count(r => r.Status == "Pending");
    
    // Calculate performance indicators
    var occupancyRate = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;
    var collectionRate = totalMonthlyRent > 0 ? (double)collectedRent / (double)totalMonthlyRent * 100 : 0;
    var completedRequests = maintenanceRequests.Count(r => r.Status == "Completed");
    var totalRequests = maintenanceRequests.Count();
    var maintenanceResponseRate = totalRequests > 0 ? (double)completedRequests / totalRequests * 100 : 0;

    var model = new PropertyManagement.Web.ViewModels.DashboardViewModel
    {
      // Property Overview
      TotalRooms = totalRooms,
      AvailableRooms = availableRooms,
      OccupiedRooms = occupiedRooms,
      UnderMaintenanceRooms = underMaintenanceRooms,
      
      // Tenant & Leasing
      TotalTenants = tenants.Count(),
      ActiveLeases = activeLeases.Count,
      ExpiringLeases = leases.Count(l => l.EndDate > now && l.EndDate <= now.AddDays(30)),
      PendingRequests = pendingRequests,
      
      // Financial Metrics
      TotalMonthlyRent = totalMonthlyRent,
      CollectedRent = collectedRent,
      OutstandingRent = outstandingRent,
      TenantsWithOutstandingBalance = GetTenantsWithOutstandingBalance(tenants, payments, activeLeases),
      
      // Operational Metrics
      MaintenanceRequestsToday = maintenanceRequestsToday,
      InspectionsDueThisWeek = inspectionsDueThisWeek,
      NewBookingsThisWeek = newBookingsThisWeek,
      RoomsNeedingAttention = underMaintenanceRooms + pendingRequests,
      
      // Performance Indicators
      OccupancyRate = occupancyRate,
      CollectionRate = collectionRate,
      MaintenanceResponseRate = maintenanceResponseRate,
      
      // Recent Activities
      RecentActivities = GetRecentActivities(payments, maintenanceRequests, bookingRequests, leases),
      
      // Quick Actions
      QuickActions = GetQuickActions(),
      
      // Alerts
      Alerts = GetAlerts(leases, maintenanceRequests, outstandingRent, now)
    };

    return View(model);
  }
  
  private int GetTenantsWithOutstandingBalance(IEnumerable<Tenant> tenants, IEnumerable<Payment> payments, List<LeaseAgreement> activeLeases)
  {
    var currentMonth = DateTime.Now.Month;
    var currentYear = DateTime.Now.Year;
    
    return tenants.Count(tenant =>
    {
      var lease = activeLeases.FirstOrDefault(l => l.TenantId == tenant.TenantId);
      if (lease == null) return false;
      
      var paidThisMonth = payments
        .Where(p => p.TenantId == tenant.TenantId && 
                   p.Date.Month == currentMonth && 
                   p.Date.Year == currentYear &&
                   p.Type == "Rent")
        .Sum(p => p.Amount);
      
      return paidThisMonth < lease.RentAmount;
    });
  }
  
  private List<PropertyManagement.Web.ViewModels.RecentActivityItem> GetRecentActivities(
    IEnumerable<Payment> payments, 
    IEnumerable<MaintenanceRequest> maintenanceRequests, 
    IEnumerable<BookingRequest> bookingRequests,
    IEnumerable<LeaseAgreement> leases)
  {
    var activities = new List<PropertyManagement.Web.ViewModels.RecentActivityItem>();
    
    // Recent payments
    activities.AddRange(payments
      .Where(p => p.Date >= DateTime.Now.AddDays(-7))
      .OrderByDescending(p => p.Date)
      .Take(3)
      .Select(p => new PropertyManagement.Web.ViewModels.RecentActivityItem
      {
        Type = "Payment",
        Description = $"Payment of {p.Amount:C} received from {p.Tenant?.FullName ?? "Unknown"}",
        Timestamp = p.Date,
        Icon = "bi-credit-card",
        Url = "/Payments"
      }));
    
    // Recent maintenance requests
    activities.AddRange(maintenanceRequests
      .Where(m => m.RequestDate >= DateTime.Now.AddDays(-7))
      .OrderByDescending(m => m.RequestDate)
      .Take(3)
      .Select(m => new PropertyManagement.Web.ViewModels.RecentActivityItem
      {
        Type = "Maintenance",
        Description = $"New maintenance request: {m.Description}",
        Timestamp = m.RequestDate,
        Icon = "bi-tools",
        Url = "/Maintenance"
      }));
    
    // Recent bookings
    activities.AddRange(bookingRequests
      .Where(b => b.RequestDate >= DateTime.Now.AddDays(-7))
      .OrderByDescending(b => b.RequestDate)
      .Take(2)
      .Select(b => new PropertyManagement.Web.ViewModels.RecentActivityItem
      {
        Type = "Booking",
        Description = $"New booking request from {b.FullName}",
        Timestamp = b.RequestDate,
        Icon = "bi-calendar-check",
        Url = "/Bookings"
      }));
    
    return activities.OrderByDescending(a => a.Timestamp).Take(8).ToList();
  }
  
  private List<PropertyManagement.Web.ViewModels.QuickActionItem> GetQuickActions()
  {
    return new List<PropertyManagement.Web.ViewModels.QuickActionItem>
    {
      new() { Title = "Add New Tenant", Description = "Register a new tenant", Url = "/Tenants/AddTenant", Icon = "bi-person-plus", Color = "primary" },
      new() { Title = "Add Room", Description = "Add a new room to the property", Url = "/Rooms", Icon = "bi-house-add", Color = "success" },
      new() { Title = "Record Payment", Description = "Record a new rent payment", Url = "/Payments", Icon = "bi-credit-card", Color = "info" },
      new() { Title = "Create Lease", Description = "Generate a new lease agreement", Url = "/LeaseAgreements", Icon = "bi-file-earmark-text", Color = "warning" },
      new() { Title = "Schedule Inspection", Description = "Schedule a room inspection", Url = "/Inspections", Icon = "bi-search", Color = "secondary" },
      new() { Title = "View Reports", Description = "Generate property reports", Url = "/Reports", Icon = "bi-graph-up", Color = "primary" }
    };
  }
  
  private List<PropertyManagement.Web.ViewModels.AlertItem> GetAlerts(
    IEnumerable<LeaseAgreement> leases, 
    IEnumerable<MaintenanceRequest> maintenanceRequests, 
    decimal outstandingRent,
    DateTime now)
  {
    var alerts = new List<PropertyManagement.Web.ViewModels.AlertItem>();
    
    // Expiring leases alert
    var expiringCount = leases.Count(l => l.EndDate > now && l.EndDate <= now.AddDays(30));
    if (expiringCount > 0)
    {
      alerts.Add(new PropertyManagement.Web.ViewModels.AlertItem
      {
        Type = "warning",
        Title = "Leases Expiring Soon",
        Message = $"{expiringCount} lease(s) expiring in the next 30 days",
        Icon = "bi-exclamation-triangle",
        Url = "/LeaseAgreements",
        CreatedAt = now
      });
    }
    
    // Overdue maintenance requests
    var overdueCount = maintenanceRequests.Count(m => m.Status == "Pending" && m.RequestDate < now.AddDays(-7));
    if (overdueCount > 0)
    {
      alerts.Add(new PropertyManagement.Web.ViewModels.AlertItem
      {
        Type = "danger",
        Title = "Overdue Maintenance",
        Message = $"{overdueCount} maintenance request(s) pending for over a week",
        Icon = "bi-exclamation-circle",
        Url = "/Maintenance",
        CreatedAt = now
      });
    }
    
    // Outstanding rent alert
    if (outstandingRent > 0)
    {
      alerts.Add(new PropertyManagement.Web.ViewModels.AlertItem
      {
        Type = "info",
        Title = "Outstanding Rent",
        Message = $"{outstandingRent:C} in outstanding rent this month",
        Icon = "bi-currency-dollar",
        Url = "/Payments",
        CreatedAt = now
      });
    }
    
    return alerts;
  }

  public IActionResult Privacy()
  {
    return View();
  }

  [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
  public IActionResult Error()
  {
    return View(new PropertyManagement.Web.ViewModels.ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}