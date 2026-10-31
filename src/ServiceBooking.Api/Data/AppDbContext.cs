using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Domain;

namespace ServiceBooking.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceType>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.CustomerName).HasMaxLength(100).IsRequired();
            entity.Property(b => b.CustomerEmail).HasMaxLength(200).IsRequired();
            entity.Property(b => b.CustomerPhone).HasMaxLength(30).IsRequired();
            entity.Property(b => b.LicensePlate).HasMaxLength(10).IsRequired();
            entity.Property(b => b.Note).HasMaxLength(500);

            // Most queries filter by time, so index it.
            entity.HasIndex(b => b.StartTime);

            entity.HasOne(b => b.ServiceType)
                .WithMany()
                .HasForeignKey(b => b.ServiceTypeId);
        });
    }
}
