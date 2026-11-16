using System.Collections.Generic;

namespace CareFlow.Models;

public sealed class Patient
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public ICollection<Booking> Bookings { get; } = new List<Booking>();
}
