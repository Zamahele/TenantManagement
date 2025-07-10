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
    }
}
