using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace DevBar;

public partial class PreferencesWindow : Window
{
    public event Action<AppSettings>? SettingsSaved;

    public PreferencesWindow(AppSettings settings)
    {
        InitializeComponent();

        var logoPath = IconRenderer.SaveAppLogoPng();
        Icon = new BitmapImage(new Uri(logoPath));

        UrlBox.Text = settings.Url;
        RefreshBox.Text = settings.RefreshSeconds.ToString("F1");
        StartupBox.IsChecked = settings.StartWithWindows;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var settings = new AppSettings
        {
            Url = UrlBox.Text.Trim(),
            RefreshSeconds = float.TryParse(RefreshBox.Text, out var r) ? Math.Max(r, 0.1f) : 1.0f,
            StartWithWindows = StartupBox.IsChecked == true,
        };

        settings.Save();
        SetAutoStart(settings.StartWithWindows);
        SettingsSaved?.Invoke(settings);
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();

    private static void SetAutoStart(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (key is null) return;

        if (enable)
        {
            var exePath = Environment.ProcessPath;
            if (exePath is not null)
                key.SetValue("DevBar", exePath);
        }
        else
        {
            key.DeleteValue("DevBar", throwOnMissingValue: false);
        }
    }
}
