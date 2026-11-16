using System;
using System.Collections.Generic;

namespace CareFlow.Models;

public sealed class Booking
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DateOnly BookingDate { get; set; }
    public ICollection<BookingService> BookingServices { get; } = new List<BookingService>();
}
