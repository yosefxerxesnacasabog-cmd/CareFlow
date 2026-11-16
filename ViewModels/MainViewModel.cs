using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CareFlow.Models;
using CareFlow.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;

namespace CareFlow.ViewModels;

public partial class MainViewModel(
    DiagnosticAvailabilityService availabilityService,
    PatientLookupService patientLookupService) : ViewModelBase
{
    private CancellationTokenSource? availabilityRequest;

    [ObservableProperty]
    public partial DateTimeOffset? SelectedDate { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial IReadOnlyList<BookingServiceOption> Availability { get; private set; } = [];

    [ObservableProperty]
    public partial string? AvailabilityMessage { get; private set; }

    [ObservableProperty]
    public partial bool IsLoadingAvailability { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<Patient> Patients { get; private set; } = [];

    [ObservableProperty]
    public partial Patient? SelectedPatient { get; set; }

    [ObservableProperty]
    public partial string? PatientMessage { get; private set; }

    [ObservableProperty]
    public partial string? ValidationMessage { get; private set; }

    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        ValidationMessage = null;
        _ = RefreshAvailabilityAsync();
    }

    partial void OnSelectedPatientChanged(Patient? value) => ValidationMessage = null;

    public Task InitializeAsync() => Task.WhenAll(LoadPatientsAsync(), RefreshAvailabilityAsync());

    private async Task LoadPatientsAsync()
    {
        try
        {
            Patients = await patientLookupService.GetPatientsAsync();
            PatientMessage = Patients.Count == 0 ? "No patients are available." : null;
        }
        catch (SqliteException error)
        {
            Trace.TraceError(error.ToString());
            PatientMessage = "Could not load patients. Check that the database is initialized, then reopen CareFlow.";
        }
    }

    [RelayCommand]
    private void ValidateBooking()
    {
        if (IsLoadingAvailability)
        {
            ValidationMessage = "Wait for the diagnostic services to finish loading.";
            return;
        }

        var date = SelectedDate is { } selectedDate
            ? DateOnly.FromDateTime(selectedDate.DateTime)
            : (DateOnly?)null;
        var serviceIds = Availability.Where(option => option.IsSelected)
            .Select(option => option.Availability.ServiceId)
            .ToArray();
        ValidationMessage = BookingRequestValidator.Validate(
            SelectedPatient?.Id, date, serviceIds, DateOnly.FromDateTime(DateTime.Today))
            ?? "Booking details are valid.";
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    public async Task RefreshAvailabilityAsync()
    {
        availabilityRequest?.Cancel();
        ValidationMessage = null;

        if (SelectedDate is not { } selectedDate)
        {
            Availability = [];
            IsLoadingAvailability = false;
            AvailabilityMessage = "Select a date to view available slots.";
            return;
        }

        using var request = new CancellationTokenSource();
        availabilityRequest = request;
        IsLoadingAvailability = true;
        AvailabilityMessage = "Loading availability…";

        try
        {
            var date = DateOnly.FromDateTime(selectedDate.DateTime);
            var results = await availabilityService.GetAvailabilityAsync(date, request.Token);

            if (!request.IsCancellationRequested)
            {
                var selectedIds = Availability.Where(option => option.IsSelected)
                    .Select(option => option.Availability.ServiceId)
                    .ToHashSet();
                Availability = results.Select(row =>
                {
                    var option = new BookingServiceOption(row)
                    {
                        IsSelected = selectedIds.Contains(row.ServiceId),
                    };
                    option.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(BookingServiceOption.IsSelected))
                        {
                            ValidationMessage = null;
                        }
                    };
                    return option;
                }).ToArray();
                AvailabilityMessage = results.Count == 0 ? "No diagnostic services are configured." : null;
            }
        }
        catch (OperationCanceledException) when (request.IsCancellationRequested)
        {
        }
        catch (SqliteException error)
        {
            Trace.TraceError(error.ToString());
            if (!request.IsCancellationRequested)
            {
                Availability = [];
                AvailabilityMessage = "Could not load availability. Check that the CareFlow database is initialized, then refresh.";
            }
        }
        finally
        {
            if (ReferenceEquals(availabilityRequest, request))
            {
                availabilityRequest = null;
                IsLoadingAvailability = false;
            }
        }
    }
}
