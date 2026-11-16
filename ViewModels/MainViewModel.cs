using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CareFlow.Models;
using CareFlow.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;

namespace CareFlow.ViewModels;

public partial class MainViewModel(DiagnosticAvailabilityService availabilityService) : ViewModelBase
{
    private CancellationTokenSource? availabilityRequest;

    [ObservableProperty]
    public partial DateTimeOffset? SelectedDate { get; set; } = DateTimeOffset.Now;

    [ObservableProperty]
    public partial IReadOnlyList<DiagnosticServiceAvailability> Availability { get; private set; } = [];

    [ObservableProperty]
    public partial string? AvailabilityMessage { get; private set; }

    partial void OnSelectedDateChanged(DateTimeOffset? value) => _ = RefreshAvailabilityAsync();

    [RelayCommand(AllowConcurrentExecutions = true)]
    public async Task RefreshAvailabilityAsync()
    {
        availabilityRequest?.Cancel();
        Availability = [];

        if (SelectedDate is not { } selectedDate)
        {
            AvailabilityMessage = "Select a date to view available slots.";
            return;
        }

        using var request = new CancellationTokenSource();
        availabilityRequest = request;
        AvailabilityMessage = "Loading availability…";

        try
        {
            var date = DateOnly.FromDateTime(selectedDate.DateTime);
            var results = await availabilityService.GetAvailabilityAsync(date, request.Token);

            if (!request.IsCancellationRequested)
            {
                Availability = results;
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
                AvailabilityMessage = "Could not load availability. Check that the CareFlow database is initialized, then refresh.";
            }
        }
        finally
        {
            if (ReferenceEquals(availabilityRequest, request))
            {
                availabilityRequest = null;
            }
        }
    }
}
