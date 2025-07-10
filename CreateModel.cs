using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyManagement.Domain.Entities;
using PropertyManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public CreateModel(ApplicationDbContext context) => _context = context;

    [BindProperty]
    public MaintenanceRequest MaintenanceRequest { get; set; }
    public List<Room> Rooms { get; set; }

    public async Task OnGetAsync()
    {
        Rooms = await _context.Rooms.ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        MaintenanceRequest.RequestDate = DateTime.UtcNow;
        MaintenanceRequest.Status = "Pending";
        _context.MaintenanceRequests.Add(MaintenanceRequest);
        await _context.SaveChangesAsync();
        return RedirectToPage("/Maintenance/Index");
    }
}