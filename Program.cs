using System.Windows.Forms;
using System.Windows.Threading;

namespace DevBar;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, @"Global\DevBar.Win", out bool isNew);
        if (!isNew) return;

        // Initialize WPF subsystem so WPF windows work inside the WinForms message loop
        var wpfApp = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };

        LogUnhandledExceptions(wpfApp.Dispatcher);
        AppLog.Write($"DevBar {Application.ProductVersion} started");

        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.Run(new DevBarContext());
    }

    // Only records the exception; the process still goes down as before, but the
    // stack trace ends up in devbar.log instead of nowhere.
    private static void LogUnhandledExceptions(Dispatcher wpfDispatcher)
    {
        wpfDispatcher.UnhandledException += (_, e) =>
            AppLog.Write($"Unhandled UI exception: {e.Exception}");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AppLog.Write($"Unhandled exception (terminating: {e.IsTerminating}): {e.ExceptionObject}");
    }
}
