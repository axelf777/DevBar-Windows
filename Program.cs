using System.Windows.Forms;

namespace DevBar;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, @"Global\DevBar.Win", out bool isNew);
        if (!isNew) return;

        // Initialize WPF subsystem so WPF windows work inside WinForms message loop
        _ = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };

        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.Run(new DevBarContext());
    }
}
