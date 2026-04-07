using System.IO;
using System.Windows.Forms;

namespace DevBar;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, @"Global\DevBar.Win", out bool isNew);
        if (!isNew) return;

        // Generate app icon if it doesn't exist yet
        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevBar");
        Directory.CreateDirectory(appDir);
        var icoPath = Path.Combine(appDir, "devbar.ico");
        if (!File.Exists(icoPath))
            IconRenderer.GenerateAppIco(icoPath);

        // Initialize WPF subsystem so WPF windows work inside WinForms message loop
        _ = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };

        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.Run(new DevBarContext());
    }
}
