using Microsoft.Win32;

namespace DevBar;

public sealed class ThemeWatcher
{
    public event Action? ThemeChanged;

    public bool IsLightTaskbar { get; private set; }

    public ThemeWatcher()
    {
        IsLightTaskbar = ReadFromRegistry();
    }

    public void Tick()
    {
        var current = ReadFromRegistry();
        if (current == IsLightTaskbar) return;
        IsLightTaskbar = current;
        ThemeChanged?.Invoke();
    }

    private static bool ReadFromRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return (key?.GetValue("SystemUsesLightTheme") as int?) == 1;
        }
        catch
        {
            return false;
        }
    }
}
