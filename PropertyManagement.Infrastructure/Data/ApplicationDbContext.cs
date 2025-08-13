using Microsoft.EntityFrameworkCore;
using PropertyManagement.Domain.Entities;

namespace PropertyManagement.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<LeaseAgreement> LeaseAgreements { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<BookingRequest> BookingRequests { get; set; }
    public DbSet<Inspection> Inspections { get; set; }
    public DbSet<UtilityBill> UtilityBills { get; set; }
    public DbSet<LeaseTemplate> LeaseTemplate { get; set; }
    public DbSet<DigitalSignature> DigitalSignature { get; set; }
    
    // Waiting List entities
    public DbSet<WaitingListEntry> WaitingListEntries { get; set; }
    public DbSet<WaitingListNotification> WaitingListNotifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User-Tenant one-to-one relationship
        modelBuilder.Entity<Tenant>()
            .HasOne(t => t.User)
            .WithOne()
            .HasForeignKey<Tenant>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Room-Tenant one-to-many relationship
        modelBuilder.Entity<Tenant>()
            .HasOne(t => t.Room)
            .WithMany(r => r.Tenants)
            .HasForeignKey(t => t.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Room-LeaseAgreement one-to-many relationship
        modelBuilder.Entity<LeaseAgreement>()
            .HasOne(l => l.Room)
            .WithMany()
            .HasForeignKey(l => l.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-LeaseAgreement one-to-many relationship
        modelBuilder.Entity<LeaseAgreement>()
            .HasOne(l => l.Tenant)
            .WithMany(t => t.LeaseAgreements)
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // LeaseTemplate-LeaseAgreement one-to-many relationship (optional)
        modelBuilder.Entity<LeaseAgreement>()
            .HasOne(l => l.LeaseTemplate)
            .WithMany()
            .HasForeignKey(l => l.LeaseTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        // LeaseAgreement-DigitalSignature one-to-many relationship
        modelBuilder.Entity<DigitalSignature>()
            .HasOne(ds => ds.LeaseAgreement)
            .WithMany(l => l.DigitalSignatures)
            .HasForeignKey(ds => ds.LeaseAgreementId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tenant-DigitalSignature one-to-many relationship (restricted to avoid cascade path conflicts)
        modelBuilder.Entity<DigitalSignature>()
            .HasOne(ds => ds.Tenant)
            .WithMany()
            .HasForeignKey(ds => ds.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tenant-Payment one-to-many relationship
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Tenant)
            .WithMany(t => t.Payments)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // LeaseAgreement-Payment (optional) relationship
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.LeaseAgreement)
            .WithMany()
            .HasForeignKey(p => p.LeaseAgreementId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UtilityBill>(entity =>
        {
            entity.Property(e => e.WaterUsage).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ElectricityUsage).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });

        // Waiting List relationships
        modelBuilder.Entity<WaitingListEntry>(entity =>
        {
            entity.HasKey(w => w.WaitingListId);
            entity.Property(w => w.PhoneNumber).IsRequired().HasMaxLength(15);
            entity.Property(w => w.FullName).HasMaxLength(100);
            entity.Property(w => w.Email).HasMaxLength(100);
            entity.Property(w => w.PreferredRoomType).HasMaxLength(20);
            entity.Property(w => w.MaxBudget).HasColumnType("decimal(18,2)");
            entity.Property(w => w.Status).IsRequired().HasMaxLength(20);
            entity.Property(w => w.Notes).HasMaxLength(500);
            entity.Property(w => w.Source).HasMaxLength(50);
        });

        // WaitingListEntry-WaitingListNotification one-to-many relationship
        modelBuilder.Entity<WaitingListNotification>()
            .HasOne(wn => wn.WaitingListEntry)
            .WithMany(wl => wl.Notifications)
            .HasForeignKey(wn => wn.WaitingListId)
            .OnDelete(DeleteBehavior.Cascade);

        // Room-WaitingListNotification relationship (optional)
        modelBuilder.Entity<WaitingListNotification>()
            .HasOne(wn => wn.Room)
            .WithMany()
            .HasForeignKey(wn => wn.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WaitingListNotification>(entity =>
        {
            entity.HasKey(wn => wn.NotificationId);
            entity.Property(wn => wn.MessageContent).IsRequired().HasMaxLength(1000);
            entity.Property(wn => wn.Status).IsRequired().HasMaxLength(20);
            entity.Property(wn => wn.Response).HasMaxLength(100);
        });
    }
}
