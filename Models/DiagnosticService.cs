using System.Collections.Generic;

namespace CareFlow.Models;

public sealed class DiagnosticService
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DailyCapacity { get; set; }
    public ICollection<BookingService> BookingServices { get; } = new List<BookingService>();
}
