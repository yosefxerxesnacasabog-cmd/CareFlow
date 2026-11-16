namespace CareFlow.Models;

public sealed class BookingService
{
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int DiagnosticServiceId { get; set; }
    public DiagnosticService DiagnosticService { get; set; } = null!;
}
