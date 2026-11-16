using CareFlow.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CareFlow.ViewModels;

public partial class BookingServiceOption(DiagnosticServiceAvailability availability) : ViewModelBase
{
    public DiagnosticServiceAvailability Availability { get; } = availability;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
