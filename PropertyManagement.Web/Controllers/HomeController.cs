using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Models;
using System.Diagnostics;

namespace PropertyManagement.Web.Controllers;

[Authorize(Roles = "Manager")]
public class HomeController : Controller
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<HomeController> _logger;

  public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
  {
    _context = context ?? throw new ArgumentNullException(nameof(context));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }


  public async Task<IActionResult> Index()
  {
    _logger.LogInformation("Accessing the dashboard at {Time}", DateTime.UtcNow);
    var now = DateTime.UtcNow;
    var model = new DashboardViewModel
    {
      TotalRooms = await _context.Rooms.CountAsync(),
      AvailableRooms = await _context.Rooms.CountAsync(r => r.Status == "Available"),
      OccupiedRooms = await _context.Rooms.CountAsync(r => r.Status == "Occupied"),
      UnderMaintenanceRooms = await _context.Rooms.CountAsync(r => r.Status == "Under Maintenance"),
      TotalTenants = await _context.Tenants.CountAsync(),
      ActiveLeases = await _context.LeaseAgreements.CountAsync(l => l.EndDate >= now),
      ExpiringLeases = await _context.LeaseAgreements.CountAsync(l => l.EndDate > now && l.EndDate <= now.AddDays(30)),
      PendingRequests = await _context.MaintenanceRequests.CountAsync(r => r.Status == "Pending")
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
    return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
  }
}