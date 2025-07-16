using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Repositories;
using System.Diagnostics;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = "Manager")]
public class HomeController : Controller
{
  private readonly IGenericRepository<Room> _roomRepository;
  private readonly IGenericRepository<Tenant> _tenantRepository;
  private readonly IGenericRepository<LeaseAgreement> _leaseAgreementRepository;
  private readonly IGenericRepository<MaintenanceRequest> _maintenanceRequestRepository;
  private readonly ILogger<HomeController> _logger;
  private readonly IMapper _mapper;

  public HomeController(
      IGenericRepository<Room> roomRepository,
      IGenericRepository<Tenant> tenantRepository,
      IGenericRepository<LeaseAgreement> leaseAgreementRepository,
      IGenericRepository<MaintenanceRequest> maintenanceRequestRepository,
      ILogger<HomeController> logger,
      IMapper mapper)
  {
    _roomRepository = roomRepository;
    _tenantRepository = tenantRepository;
    _leaseAgreementRepository = leaseAgreementRepository;
    _maintenanceRequestRepository = maintenanceRequestRepository;
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
  }

  // Add a static counter for home page visits
  private static readonly Counter HomePageVisitCounter =
      Metrics.CreateCounter("home_page_visits_total", "Total number of visits to the home page.");

  public async Task<IActionResult> Index()
  {
    // Increment the Prometheus counter
    HomePageVisitCounter.Inc();

    _logger.LogInformation("Accessing the dashboard at {Time}", DateTime.UtcNow);
    var now = DateTime.UtcNow;

    var rooms = await _roomRepository.GetAllAsync();
    var tenants = await _tenantRepository.GetAllAsync();
    var leases = await _leaseAgreementRepository.GetAllAsync();
    var pendingRequests = await _maintenanceRequestRepository.GetAllAsync(r => r.Status == "Pending");

    var model = new PropertyManagement.Web.ViewModels.DashboardViewModel
    {
      TotalRooms = rooms.Count(),
      AvailableRooms = rooms.Count(r => r.Status == "Available"),
      OccupiedRooms = rooms.Count(r => r.Status == "Occupied"),
      UnderMaintenanceRooms = rooms.Count(r => r.Status == "Under Maintenance"),
      TotalTenants = tenants.Count(),
      ActiveLeases = leases.Count(l => l.EndDate >= now),
      ExpiringLeases = leases.Count(l => l.EndDate > now && l.EndDate <= now.AddDays(30)),
      PendingRequests = pendingRequests.Count()
    };

    return View(model);
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