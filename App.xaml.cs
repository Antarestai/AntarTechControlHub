using System.Configuration;
using System.Data;
using System.Windows;

namespace AntarTechControl;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            try { System.IO.File.WriteAllText("crash.log", args.ExceptionObject?.ToString() ?? "Unknown exception"); } catch { }
        };
        DispatcherUnhandledException += (s, args) =>
        {
            try { System.IO.File.WriteAllText("crash.log", args.Exception?.ToString() ?? "Unknown dispatcher exception"); } catch { }
        };
        base.OnStartup(e);
    }
}

