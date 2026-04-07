using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;

namespace DevBar;

public class DevBarContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string _appLogoPath;
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevBar", "devbar.log");

    private AppSettings _settings;
    private DevBarResult? _lastResult;
    private bool _popupOpen;
    private bool _requestInFlight;
    private DateTime _lastFetchTime = DateTime.MinValue;
    private bool _hasShownPreferences;
    private PopupWindow? _popupWindow;
    private ContextPopup? _contextPopup;
    private Icon _currentIcon;

    private const double BatteryPollingInterval = 30.0;

    public DevBarContext()
    {
        _settings = AppSettings.Load();
        _currentIcon = IconRenderer.Create(IconState.ServerDown);
        _appLogoPath = IconRenderer.SaveAppLogoPng();

        _trayIcon = new NotifyIcon
        {
            Icon = _currentIcon,
            Visible = true,
            Text = "DevBar",
        };
        _trayIcon.MouseUp += OnTrayMouseUp;

        var interval = Math.Max(_settings.RefreshSeconds, 0.1f);
        _timer = new System.Windows.Forms.Timer { Interval = (int)(interval * 1000) };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        ToastNotificationManagerCompat.OnActivated += OnToastActivated;

        OnTimerTick(this, EventArgs.Empty);
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        try { await Update(); }
        catch (Exception ex) { Log($"Update error: {ex}"); }
    }

    private void OnTrayMouseUp(object? sender, MouseEventArgs e)
    {
        try
        {
            if (e.Button == MouseButtons.Left)
                TogglePopup();
            else if (e.Button == MouseButtons.Right)
                ShowContextMenu();
        }
        catch (Exception ex) { Log($"Click error: {ex}"); }
    }

    private void ShowContextMenu()
    {
        CloseAllPopups();

        _contextPopup = new ContextPopup();
        _contextPopup.Deactivated += (_, _) => CloseContextPopup();
        _contextPopup.PreferencesClicked += ShowPreferences;
        _contextPopup.RefreshClicked += async () =>
        {
            try { await Update(); }
            catch (Exception ex) { Log($"Refresh error: {ex}"); }
        };
        _contextPopup.QuitClicked += ExitThread;

        PositionAndShow(_contextPopup);
    }

    private void TogglePopup()
    {
        // If popup is already showing, just close it
        if (_popupOpen)
        {
            CloseAllPopups();
            return;
        }

        if (_lastResult is null) return;

        CloseAllPopups();

        _popupWindow = new PopupWindow(_lastResult);
        _popupWindow.Deactivated += (_, _) => CloseMainPopup();
        _popupWindow.ItemClicked += url =>
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { Log($"Open URL error: {ex}"); }
        };

        PositionAndShow(_popupWindow);
        _popupOpen = true;
    }

    private void PositionAndShow(System.Windows.Window window)
    {
        window.Left = -10000;
        window.Top = -10000;
        window.Show();
        window.UpdateLayout();

        var cursor = System.Windows.Forms.Cursor.Position;
        var workArea = System.Windows.SystemParameters.WorkArea;
        var w = window.ActualWidth;
        var h = window.ActualHeight;

        window.Left = Math.Max(workArea.Left, Math.Min(cursor.X - w / 2, workArea.Right - w));
        window.Top = Math.Max(workArea.Top, workArea.Bottom - h);
        window.Activate();
    }

    private void CloseMainPopup()
    {
        if (_popupWindow is null) return;
        var win = _popupWindow;
        _popupWindow = null;
        _popupOpen = false;
        try { win.Close(); } catch { /* already closing */ }
    }

    private void CloseContextPopup()
    {
        if (_contextPopup is null) return;
        var win = _contextPopup;
        _contextPopup = null;
        try { win.Close(); } catch { /* already closing */ }
    }

    private void CloseAllPopups()
    {
        CloseMainPopup();
        CloseContextPopup();
    }

    private async Task Update()
    {
        if (_popupOpen || _requestInFlight) return;

        if (IsOnBattery() && (DateTime.Now - _lastFetchTime).TotalSeconds < BatteryPollingInterval)
            return;

        if (string.IsNullOrWhiteSpace(_settings.Url))
        {
            if (!_hasShownPreferences)
            {
                ShowPreferences();
                _hasShownPreferences = true;
            }
            UpdateIcon(null);
            return;
        }

        _requestInFlight = true;
        _lastFetchTime = DateTime.Now;

        try
        {
            var url = _settings.Url + $"?username={Environment.UserName}";
            var response = await _http.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                return;

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DevBarResult>(json);

            if (result is not null)
            {
                NotifyNewItems(_lastResult, result);
                _lastResult = result;
                UpdateIcon(result);
            }
        }
        catch (Exception ex)
        {
            Log($"Fetch error: {ex.Message}");
            UpdateIcon(null);
        }
        finally
        {
            _requestInFlight = false;
        }
    }

    private void UpdateIcon(DevBarResult? result)
    {
        IconState state;
        string tooltip;

        if (result is null)
        {
            state = IconState.ServerDown;
            tooltip = "DevBar — server unreachable";
        }
        else
        {
            var totalItems = result.Data.Values.Sum(list => list.Count);
            var hasHighPriority = result.Data.Any(kv =>
                kv.Value.Count > 0 &&
                result.Metadata.Display.TryGetValue(kv.Key, out var d) && d.Priority < 10);

            if (totalItems == 0)
            {
                state = IconState.AllClear;
                tooltip = "DevBar — all clear";
            }
            else if (hasHighPriority)
            {
                state = IconState.HighPriority;
                tooltip = BuildTooltip(result);
            }
            else
            {
                state = IconState.HasItems;
                tooltip = BuildTooltip(result);
            }
        }

        var newIcon = IconRenderer.Create(state);
        _trayIcon.Icon = newIcon;
        _trayIcon.Text = tooltip;
        _currentIcon.Dispose();
        _currentIcon = newIcon;
    }

    private static string BuildTooltip(DevBarResult result)
    {
        var lines = new List<string> { "DevBar" };
        foreach (var (category, items) in result.Data
            .Where(kv => kv.Value.Count > 0)
            .OrderBy(kv => result.Metadata.Display.TryGetValue(kv.Key, out var d) ? d.Priority : 99))
        {
            var title = result.Metadata.Display.TryGetValue(category, out var d) ? d.Title : category;
            lines.Add($"{title}: {items.Count}");
        }
        var tooltip = string.Join("\n", lines);
        return tooltip.Length > 127 ? tooltip[..127] : tooltip;
    }

    private void NotifyNewItems(DevBarResult? previous, DevBarResult current)
    {
        try
        {
            var newItems = ItemDiffer.GetNewItems(previous, current);
            if (newItems.Count == 0) return;

            if (newItems.Count <= 3)
            {
                foreach (var (_, symbol, item) in newItems)
                {
                    new ToastContentBuilder()
                        .AddAppLogoOverride(new Uri(_appLogoPath), ToastGenericAppLogoCrop.Circle)
                        .AddText($"{symbol} {item.Title}")
                        .AddArgument("url", item.Url)
                        .Show();
                }
            }
            else
            {
                var categories = newItems.Select(n => n.Category).Distinct().Count();
                new ToastContentBuilder()
                    .AddAppLogoOverride(new Uri(_appLogoPath), ToastGenericAppLogoCrop.Circle)
                    .AddText($"{newItems.Count} new items across {categories} categories")
                    .Show();
            }
        }
        catch (Exception ex)
        {
            Log($"Toast error: {ex.Message}");
        }
    }

    private static void OnToastActivated(ToastNotificationActivatedEventArgsCompat e)
    {
        try
        {
            var args = ToastArguments.Parse(e.Argument);
            if (args.TryGetValue("url", out var url))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { /* best effort */ }
    }

    private void ShowPreferences()
    {
        var window = new PreferencesWindow(_settings);
        window.SettingsSaved += newSettings =>
        {
            _settings = newSettings;
            var interval = Math.Max(_settings.RefreshSeconds, 0.1f);
            _timer.Interval = (int)(interval * 1000);
        };
        window.Show();
        window.Activate();
    }

    private static bool IsOnBattery()
    {
        return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline;
    }

    private static void Log(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
        catch { /* logging must never crash the app */ }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            _trayIcon.Dispose();
            _currentIcon.Dispose();
            _http.Dispose();
            ToastNotificationManagerCompat.Uninstall();
        }
        base.Dispose(disposing);
    }
}
