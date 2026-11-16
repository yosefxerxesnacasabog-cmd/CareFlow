using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CareFlow.Models;
using CareFlow.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;

namespace CareFlow.ViewModels;

public partial class BookingHistoryViewModel(BookingHistoryService historyService) : ViewModelBase
{
    private long refreshVersion;

    [ObservableProperty]
    public partial IReadOnlyList<BookingHistoryEntry> Entries { get; private set; } = [];

    [ObservableProperty]
    public partial string? Message { get; private set; }

    [ObservableProperty]
    public partial bool IsLoading { get; private set; }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var version = ++refreshVersion;
        IsLoading = true;
        Message = "Loading booking history…";

        try
        {
            var entries = await historyService.GetHistoryAsync();
            if (version == refreshVersion)
            {
                Entries = entries;
                Message = entries.Count == 0 ? "No confirmed bookings yet." : null;
            }
        }
        catch (SqliteException error)
        {
            Trace.TraceError(error.ToString());
            if (version == refreshVersion)
            {
                Entries = [];
                Message = "Could not load booking history. Check that the database is initialized, then refresh.";
            }
        }
        finally
        {
            if (version == refreshVersion)
            {
                IsLoading = false;
            }
        }
    }
}
