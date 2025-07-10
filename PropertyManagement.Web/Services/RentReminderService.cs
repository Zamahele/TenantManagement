using Microsoft.EntityFrameworkCore;
using PropertyManagement.Infrastructure.Data;
using PropertyManagement.Web.Services;

public class RentReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public RentReminderService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // This method can be called from tests
    public async Task SendRemindersAsync(ApplicationDbContext db, ISmsService smsService)
    {
        var today = DateTime.Today;

        var tenantsWithLeases = db.Tenants
            .Include(t => t.LeaseAgreements)
            .ToList();

        foreach (var tenant in tenantsWithLeases)
        {
            var lease = tenant.LeaseAgreements?
                .FirstOrDefault(l => l.StartDate <= today && l.EndDate >= today);

            if (lease?.RentDueDate != null && lease.RentDueDate.Value.Date == today.AddDays(3))
            {
                var phone = tenant.Contact;
                if (!string.IsNullOrWhiteSpace(phone))
                {
                    await smsService.SendAsync(
                        phone,
                        $"Reminder: Dear {tenant.FullName}, your rent is due on {lease.RentDueDate:MMMM dd, yyyy}."
                    );
                }
            }
        }
    }

    public async Task SendManagerNotificationsAsync(ApplicationDbContext db, IEmailService emailService)
    {
        var today = DateTime.Today;

        // Bring all LeaseAgreements and Payments into memory first
        var leases = db.LeaseAgreements
            .Include(l => l.Tenant)
            .ToList();

        var payments = db.Payments.ToList();

        // Now filter in-memory using C# (not in the database)
        var overdueLeases = leases
            .Where(l => l.RentDueDate < today &&
                        !payments.Any(p =>
                            p.TenantId == l.TenantId &&
                            p.PaymentMonth == l.RentDueDate?.Month &&
                            p.PaymentYear == l.RentDueDate?.Year))
            .ToList();

        // Expiring leases: EndDate within next 30 days
        var expiringLeases = db.LeaseAgreements
            .Include(l => l.Tenant)
            .Where(l => l.EndDate <= today.AddDays(30) && l.EndDate >= today)
            .ToList();

        var managerEmail = "manager@yourcompany.com"; // Or get from config/db

        if (overdueLeases.Any())
        {
            var body = "Overdue rents:\n" + string.Join("\n", overdueLeases.Select(l => $"{l.Tenant.FullName}: Due {l.RentDueDate:MMM dd}"));
            await emailService.SendAsync(managerEmail, "Overdue Rent Notification", body);
        }

        if (expiringLeases.Any())
        {
            var body = "Expiring leases:\n" + string.Join("\n", expiringLeases.Select(l => $"{l.Tenant.FullName}: Ends {l.EndDate:MMM dd}"));
            await emailService.SendAsync(managerEmail, "Expiring Lease Notification", body);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await SendRemindersAsync(db, smsService);
                await SendManagerNotificationsAsync(db, emailService);
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
        }
    }
}