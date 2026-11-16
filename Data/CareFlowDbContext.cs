using CareFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Data;

public sealed class CareFlowDbContext(DbContextOptions<CareFlowDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DiagnosticService> DiagnosticServices => Set<DiagnosticService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(patient =>
        {
            patient.HasData(
                new Patient { Id = 1, FullName = "Mira Sample" },
                new Patient { Id = 2, FullName = "Theo Example" },
                new Patient { Id = 3, FullName = "Lina Demo" });
        });

        modelBuilder.Entity<DiagnosticService>(service =>
        {
            service.ToTable(table => table.HasCheckConstraint(
                "CK_DiagnosticServices_DailyCapacity", "DailyCapacity >= 0"));
            service.HasData(
                new DiagnosticService { Id = 1, Name = "Complete Blood Count", DailyCapacity = 8 },
                new DiagnosticService { Id = 2, Name = "Urinalysis", DailyCapacity = 6 },
                new DiagnosticService { Id = 3, Name = "Chest X-Ray", DailyCapacity = 4 },
                new DiagnosticService { Id = 4, Name = "ECG", DailyCapacity = 3 });
        });

        modelBuilder.Entity<Booking>(booking =>
        {
            booking.HasIndex(entity => entity.BookingDate);
            booking.HasOne(entity => entity.Patient)
                .WithMany(patient => patient.Bookings)
                .HasForeignKey(entity => entity.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookingService>(bookingService =>
        {
            bookingService.HasKey(entity => new { entity.BookingId, entity.DiagnosticServiceId });
            bookingService.HasOne(entity => entity.Booking)
                .WithMany(booking => booking.BookingServices)
                .HasForeignKey(entity => entity.BookingId);
            bookingService.HasOne(entity => entity.DiagnosticService)
                .WithMany(service => service.BookingServices)
                .HasForeignKey(entity => entity.DiagnosticServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
