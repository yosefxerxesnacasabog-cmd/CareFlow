using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CareFlow.Data;
using CareFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Services;

public sealed class DiagnosticAvailabilityService(IDbContextFactory<CareFlowDbContext> contextFactory)
{
    public Task<List<DiagnosticServiceAvailability>> GetAvailabilityAsync(
        DateOnly date, CancellationToken cancellationToken = default)
    {
        // Microsoft.Data.Sqlite executes I/O synchronously even through its async API.
        // Keep the query off the UI thread.
        return Task.Run(async () =>
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

            return await db.DiagnosticServices
                .OrderBy(service => service.Name)
                .Select(service => new DiagnosticServiceAvailability(
                    service.Id,
                    service.Name,
                    service.DailyCapacity,
                    Math.Max(0, service.DailyCapacity - service.BookingServices
                        .Count(entry => entry.Booking.BookingDate == date))))
                .ToListAsync(cancellationToken);
        }, cancellationToken);
    }
}
