using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CareFlow.Data;
using CareFlow.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Services;

public sealed class BookingConfirmationService(IDbContextFactory<CareFlowDbContext> contextFactory)
{
    public Task<BookingConfirmationResult> ConfirmAsync(
        int patientId, DateOnly date, IReadOnlyCollection<int> serviceIds)
    {
        var selectedIds = serviceIds.ToArray();
        return Task.Run(() => ConfirmInTransactionAsync(patientId, date, selectedIds));
    }

    private async Task<BookingConfirmationResult> ConfirmInTransactionAsync(
        int patientId, DateOnly date, int[] selectedIds)
    {
        try
        {
            await using var db = await contextFactory.CreateDbContextAsync();
            await db.Database.OpenConnectionAsync();

            // Reserve the SQLite writer before reading capacity so another confirmation
            // cannot commit a booking between the capacity check and this write.
            var connection = (SqliteConnection)db.Database.GetDbConnection();
            await using var transaction = connection.BeginTransaction(deferred: false);
            await using var enlistedTransaction = await db.Database.UseTransactionAsync(transaction);

            var validationError = BookingRequestValidator.Validate(
                patientId, date, selectedIds, DateOnly.FromDateTime(DateTime.Today));
            if (validationError is not null)
            {
                return new BookingConfirmationResult(null, validationError);
            }

            if (!await db.Patients.AnyAsync(patient => patient.Id == patientId))
            {
                return new BookingConfirmationResult(null, "Select an existing patient.");
            }

            var available = await DiagnosticAvailabilityService.QueryAvailability(
                db.DiagnosticServices.Where(service => selectedIds.Contains(service.Id)), date)
                .ToListAsync();

            if (available.Count != selectedIds.Length)
            {
                return new BookingConfirmationResult(null, "A selected diagnostic service is no longer available.");
            }

            var fullServices = available.Where(service => service.RemainingSlots == 0)
                .Select(service => service.Name)
                .ToArray();
            if (fullServices.Length > 0)
            {
                return new BookingConfirmationResult(null,
                    $"Fully booked on the selected date ({string.Join(", ", fullServices)}). Choose another date or service.");
            }

            var booking = new Booking { PatientId = patientId, BookingDate = date };
            foreach (var serviceId in selectedIds)
            {
                booking.BookingServices.Add(new BookingService { DiagnosticServiceId = serviceId });
            }

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new BookingConfirmationResult(booking.Id, null);
        }
        catch (SqliteException error) when (error.SqliteErrorCode is 5 or 6)
        {
            Trace.TraceError(error.ToString());
            return new BookingConfirmationResult(null, "The database is busy. Please try again.");
        }
        catch (Exception error) when (error is SqliteException or DbUpdateException)
        {
            Trace.TraceError(error.ToString());
            return new BookingConfirmationResult(null, "Could not save the booking. Please try again.");
        }
    }
}
