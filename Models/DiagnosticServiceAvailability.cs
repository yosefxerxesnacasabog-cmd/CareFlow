namespace CareFlow.Models;

public sealed record DiagnosticServiceAvailability(
    int ServiceId,
    string Name,
    int DailyCapacity,
    int RemainingSlots);
