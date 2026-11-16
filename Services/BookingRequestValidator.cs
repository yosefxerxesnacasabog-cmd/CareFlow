using System;
using System.Collections.Generic;
using System.Linq;

namespace CareFlow.Services;

public static class BookingRequestValidator
{
    public static string? Validate(int? patientId, DateOnly? date, IReadOnlyCollection<int> serviceIds, DateOnly today)
    {
        if (patientId is null or <= 0)
        {
            return "Select an existing patient.";
        }

        if (date is null)
        {
            return "Select a booking date.";
        }

        if (date < today)
        {
            return "Choose today or a future date.";
        }

        if (serviceIds.Distinct().Count() != serviceIds.Count)
        {
            return "Select each diagnostic service only once.";
        }

        return serviceIds.Count is < 1 or > 3
            ? "Select one to three diagnostic services."
            : null;
    }
}
