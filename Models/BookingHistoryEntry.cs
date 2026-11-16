using System;
using System.Collections.Generic;

namespace CareFlow.Models;

public sealed record BookingHistoryEntry(
    int BookingId,
    string PatientName,
    DateOnly BookingDate,
    IReadOnlyList<string> Services)
{
    public string ServiceNames => string.Join(", ", Services);
}
