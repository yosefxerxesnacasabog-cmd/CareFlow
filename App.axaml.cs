using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CareFlow.ViewModels;
using CareFlow.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CareFlow;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection()
                .AddSingleton<MainViewModel>()
                .AddSingleton<MainWindow>(provider => new MainWindow
                {
                    DataContext = provider.GetRequiredService<MainViewModel>(),
                })
                .BuildServiceProvider();

            desktop.Exit += (_, _) => services.Dispose();
            desktop.MainWindow = services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}