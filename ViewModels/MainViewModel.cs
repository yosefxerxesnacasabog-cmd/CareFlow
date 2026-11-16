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
    PatientLookupService patientLookupService,
    BookingConfirmationService bookingConfirmationService,
    BookingHistoryViewModel history) : ViewModelBase
{
    private CancellationTokenSource? availabilityRequest;

    public BookingHistoryViewModel History { get; } = history;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSelectPatient))]
    public partial bool IsConfirming { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSelectPatient))]
    public partial bool IsLoadingPatients { get; private set; }

    public bool CanSelectPatient => !IsLoadingPatients && !IsConfirming;

    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        ValidationMessage = null;
        _ = RefreshAvailabilityAsync();
    }

    partial void OnSelectedPatientChanged(Patient? value) => ValidationMessage = null;

    public Task InitializeAsync() => Task.WhenAll(LoadPatientsAsync(), RefreshAvailabilityAsync(), History.RefreshAsync());

    private async Task LoadPatientsAsync()
    {
        IsLoadingPatients = true;
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
        finally
        {
            IsLoadingPatients = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmBookingAsync()
    {
        if (IsLoadingAvailability || IsConfirming)
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
        var patientId = SelectedPatient?.Id;
        var validationError = BookingRequestValidator.Validate(
            patientId, date, serviceIds, DateOnly.FromDateTime(DateTime.Today));
        if (validationError is not null)
        {
            ValidationMessage = validationError;
            return;
        }

        IsConfirming = true;
        ValidationMessage = "Confirming booking…";

        try
        {
            var result = await bookingConfirmationService.ConfirmAsync(
                patientId!.Value, date!.Value, serviceIds);
            if (result.BookingId.HasValue)
            {
                foreach (var option in Availability)
                {
                    option.IsSelected = false;
                }
            }

            var historyRefresh = result.BookingId.HasValue ? History.RefreshAsync() : Task.CompletedTask;
            await Task.WhenAll(RefreshAvailabilityAsync(), historyRefresh);
            ValidationMessage = result.BookingId is { } bookingId
                ? $"Booking #{bookingId} confirmed."
                : result.ErrorMessage;
        }
        finally
        {
            IsConfirming = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    public async Task RefreshAvailabilityAsync()
    {
        availabilityRequest?.Cancel();
        if (!IsConfirming)
        {
            ValidationMessage = null;
        }

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
                        if (!IsConfirming && args.PropertyName == nameof(BookingServiceOption.IsSelected))
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
