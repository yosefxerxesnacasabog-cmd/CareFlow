using System.Diagnostics;
using System.Globalization;
using CareFlow.Data;
using CareFlow.Services;
using CareFlow.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

if (args is [var mode, var path, var date])
{
    using var services = CreateServices(path);
    var bookingDate = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    if (mode == "write-history")
    {
        Require(await Confirm(services, bookingDate, [1, 4]) == 1, "Expected the first booking to have ID 1.");
        return 0;
    }

    Require(mode == "read-history", "Unknown history test mode.");
    var model = services.GetRequiredService<MainViewModel>();
    await model.InitializeAsync();

    var entry = model.History.Entries.Single();
    Require(entry.BookingId == 1
        && entry.PatientName == "Mira Sample"
        && entry.BookingDate == bookingDate
        && entry.Services.SequenceEqual(new[] { "Complete Blood Count", "ECG" }),
        "A fresh process did not reload the patient, date, and all booked services.");
    return 0;
}

var testDate = DateOnly.FromDateTime(DateTime.Today).AddDays(7);
(string Name, Func<ServiceProvider, string, Task> Run)[] scenarios =
[
    ("AC-01 | Remaining capacity for the selected date", async (services, _) =>
    {
        await Confirm(services, testDate, [1, 4]);
        await Confirm(services, testDate, [4]);
        await Confirm(services, testDate.AddDays(1), [1, 2]);

        await CheckSlots(services, testDate, [4, 7, 1, 6]);
        await CheckSlots(services, testDate.AddDays(1), [4, 7, 3, 5]);
        await CheckSlots(services, testDate.AddDays(2), [4, 8, 3, 6]);
    }),
    ("AC-02 | Successful multi-service booking", async (services, _) =>
    {
        var model = services.GetRequiredService<MainViewModel>();
        await model.InitializeAsync();
        model.SelectedPatient = model.Patients.Single(patient => patient.Id == 1);
        var selectedDate = DateOnly.FromDateTime(model.SelectedDate!.Value.DateTime);
        foreach (var option in model.Availability.Where(option => option.Availability.ServiceId is 1 or 4))
        {
            option.IsSelected = true;
        }

        await model.ConfirmBookingCommand.ExecuteAsync(null);

        await using var db = await CreateContext(services);
        var persisted = await db.Bookings.Include(booking => booking.BookingServices).SingleAsync();
        Require(model.ValidationMessage == $"Booking #{persisted.Id} confirmed.", "The form did not show confirmation.");
        Require(persisted.PatientId == 1 && persisted.BookingDate == selectedDate
            && persisted.BookingServices.Select(row => row.DiagnosticServiceId).Order().SequenceEqual(new[] { 1, 4 }),
            "The booking did not persist with the correct patient, date, and two services.");
        Require(model.Availability.Single(row => row.Availability.ServiceId == 4).Availability.RemainingSlots == 2
            && model.History.Entries.Single().BookingId == persisted.Id,
            "Availability and history did not refresh after confirmation.");
    }),
    ("AC-03 | Invalid or unavailable requests are rejected", async (services, _) =>
    {
        var booking = services.GetRequiredService<BookingConfirmationService>();
        (int PatientId, DateOnly Date, int[] Services, string Message)[] invalid =
        [
            (0, testDate, [1], "Select an existing patient."),
            (999, testDate, [1], "Select an existing patient."),
            (1, testDate, [999], "A selected diagnostic service is no longer available."),
            (1, testDate.AddDays(-9), [1], "Choose today or a future date."),
            (1, testDate, [], "Select one to three diagnostic services."),
            (1, testDate, [1, 2, 3, 4], "Select one to three diagnostic services."),
            (1, testDate, [1, 1], "Select each diagnostic service only once."),
        ];
        foreach (var request in invalid)
        {
            var result = await booking.ConfirmAsync(request.PatientId, request.Date, request.Services);
            Require(result.BookingId is null && result.ErrorMessage == request.Message,
                $"Expected rejection with '{request.Message}'.");
        }
        await CheckCounts(services, 0, 0);

        await Confirm(services, testDate, [4]);
        await Confirm(services, testDate, [4]);
        await Confirm(services, testDate, [4]);
        var full = await booking.ConfirmAsync(1, testDate, [4]);
        Require(full.BookingId is null && full.ErrorMessage?.Contains("ECG") == true,
            "A request for a fully booked service was accepted.");
        await CheckCounts(services, 3, 3);
    }),
    ("AC-04 | Stale availability is rechecked", async (services, _) =>
    {
        await Confirm(services, testDate, [4]);
        await Confirm(services, testDate, [4]);
        await CheckSlots(services, testDate, [4, 8, 1, 6]);

        var booking = services.GetRequiredService<BookingConfirmationService>();
        var competingRequests = await Task.WhenAll(
            booking.ConfirmAsync(1, testDate, [4]),
            booking.ConfirmAsync(2, testDate, [4]));
        Require(competingRequests.Count(result => result.BookingId is not null) == 1
            && competingRequests.Count(result => result.ErrorMessage?.Contains("ECG") == true) == 1,
            "Two requests competing for the last slot did not produce one success and one capacity rejection.");

        var stale = await booking.ConfirmAsync(1, testDate, [1, 4]);
        Require(stale.BookingId is null && stale.ErrorMessage?.Contains("ECG") == true,
            "A multi-service request using outdated capacity was accepted.");
        await CheckCounts(services, 3, 3);
        await CheckSlots(services, testDate, [4, 8, 0, 6]);
    }),
    ("AC-05 | Failure rolls back the entire booking", async (services, _) =>
    {
        const string failure = "Forced failure after inserting the first service";
        await using (var db = await CreateContext(services))
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER FailSecondService BEFORE INSERT ON BookingServices
                WHEN EXISTS (SELECT 1 FROM BookingServices WHERE BookingId = NEW.BookingId)
                BEGIN
                    SELECT RAISE(ABORT, 'Forced failure after inserting the first service');
                END;
                """);
        }

        using var trace = new StringWriter();
        using var listener = new TextWriterTraceListener(trace);
        Trace.Listeners.Add(listener);
        try
        {
            var result = await services.GetRequiredService<BookingConfirmationService>()
                .ConfirmAsync(1, testDate, [1, 4]);
            listener.Flush();
            Require(result.BookingId is null && result.ErrorMessage?.Contains("Could not save") == true
                && trace.ToString().Contains(failure), "The intended second-service failure was not observed.");
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }

        await CheckCounts(services, 0, 0);
        await CheckSlots(services, testDate, [4, 8, 3, 6]);
    }),
    ("AC-06 | A fresh process reloads confirmed history", async (_, database) =>
    {
        await RunHistoryProcess("write-history", database, testDate);
        await RunHistoryProcess("read-history", database, testDate);
    }),
];

var failures = 0;
foreach (var (name, run) in scenarios)
{
    var database = Path.GetTempFileName();
    try
    {
        using var services = CreateServices(database);
        await using (var db = await CreateContext(services))
        {
            await db.Database.MigrateAsync();
        }
        await run(services, database);
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception error)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {name}{Environment.NewLine}{error}");
    }
    finally
    {
        File.Delete(database);
    }
}

Console.WriteLine($"{scenarios.Length - failures}/{scenarios.Length} acceptance scenarios passed.");
return failures == 0 ? 0 : 1;

static async Task RunHistoryProcess(string mode, string database, DateOnly date)
{
    var executable = Environment.ProcessPath!;
    var start = new ProcessStartInfo(executable) { UseShellExecute = false };
    if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
    {
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
    }
    start.ArgumentList.Add(mode);
    start.ArgumentList.Add(database);
    start.ArgumentList.Add(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    using var child = Process.Start(start) ?? throw new InvalidOperationException("Could not start the history check.");
    await child.WaitForExitAsync();
    Require(child.ExitCode == 0, $"The {mode} process failed its history check.");
}

static ServiceProvider CreateServices(string database)
{
    var connection = new SqliteConnectionStringBuilder
    {
        DataSource = database,
        ForeignKeys = true,
        Pooling = false,
    };
    return new ServiceCollection()
        .AddDbContextFactory<CareFlowDbContext>(options => options.UseSqlite(connection.ToString()))
        .AddSingleton<DiagnosticAvailabilityService>()
        .AddSingleton<PatientLookupService>()
        .AddSingleton<BookingConfirmationService>()
        .AddSingleton<BookingHistoryService>()
        .AddSingleton<BookingHistoryViewModel>()
        .AddSingleton<MainViewModel>()
        .BuildServiceProvider();
}

static Task<CareFlowDbContext> CreateContext(ServiceProvider services) =>
    services.GetRequiredService<IDbContextFactory<CareFlowDbContext>>().CreateDbContextAsync();

static async Task<int> Confirm(ServiceProvider services, DateOnly date, int[] ids)
{
    var result = await services.GetRequiredService<BookingConfirmationService>().ConfirmAsync(1, date, ids);
    return result.BookingId ?? throw new InvalidOperationException(result.ErrorMessage);
}

static async Task CheckSlots(ServiceProvider services, DateOnly date, int[] expected)
{
    var rows = await services.GetRequiredService<DiagnosticAvailabilityService>().GetAvailabilityAsync(date);
    Require(rows.Select(row => row.Name).SequenceEqual(new[] { "Chest X-Ray", "Complete Blood Count", "ECG", "Urinalysis" })
        && rows.Select(row => row.DailyCapacity).SequenceEqual(new[] { 4, 8, 3, 6 })
        && rows.Select(row => row.RemainingSlots).SequenceEqual(expected),
        $"Unexpected availability for {date:d} ({string.Join(", ", rows.Select(row => row.RemainingSlots))}).");
}

static async Task CheckCounts(ServiceProvider services, int bookings, int serviceLinks)
{
    await using var db = await CreateContext(services);
    Require(await db.Bookings.CountAsync() == bookings && await db.BookingServices.CountAsync() == serviceLinks,
        "The database contains an unexpected number of booking or service rows.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
