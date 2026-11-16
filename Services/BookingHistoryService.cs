using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CareFlow.Data;
using CareFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Services;

public sealed class BookingHistoryService(IDbContextFactory<CareFlowDbContext> contextFactory)
{
    public Task<List<BookingHistoryEntry>> GetHistoryAsync() => Task.Run(async () =>
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        return await db.Bookings
            .OrderByDescending(booking => booking.BookingDate)
            .ThenByDescending(booking => booking.Id)
            .Select(booking => new BookingHistoryEntry(
                booking.Id,
                booking.Patient.FullName,
                booking.BookingDate,
                booking.BookingServices
                    .OrderBy(entry => entry.DiagnosticService.Name)
                    .Select(entry => entry.DiagnosticService.Name)
                    .ToList()))
            .ToListAsync();
    });
}
