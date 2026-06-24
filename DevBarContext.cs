using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Threading;

namespace DevBar;

public class DevBarContext : ApplicationContext
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
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

    private readonly Dictionary<IntPtr, OverlayWindow> _overlays = new();
    private readonly TaskbarTracker _tracker;
    private readonly BroadcastListener _broadcast;
    private readonly WinEventHookListener _hookListener;
    private readonly ThemeWatcher _themeWatcher;
    private readonly DispatcherTimer _dataTimer;
    private readonly DispatcherTimer _taskbarTimer;
    private bool _dumpedFirstResponse;

    private const double BatteryPollingInterval = 30.0;

    public DevBarContext()
    {
        _settings = AppSettings.Load();

        _tracker = new TaskbarTracker();
        _tracker.StateChanged += ApplyTaskbarStates;

        _broadcast = new BroadcastListener();
        _broadcast.TaskbarCreated += _tracker.Tick;

        var interval = Math.Max(_settings.RefreshSeconds, 0.1f);
        _dataTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(interval) };
        _dataTimer.Tick += OnDataTimerTick;
        _dataTimer.Start();

        _themeWatcher = new ThemeWatcher();
        _themeWatcher.ThemeChanged += OnThemeChanged;

        _taskbarTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _taskbarTimer.Tick += (_, _) =>
        {
            _tracker.Tick();
            _themeWatcher.Tick();
        };
        _taskbarTimer.Start();

        _hookListener = new WinEventHookListener();
        _hookListener.ForegroundChanged += _ =>
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (var overlay in _overlays.Values)
                    if (overlay.IsVisible) overlay.ReassertTopmost();
            });

        _tracker.Tick();
        OnDataTimerTick(this, EventArgs.Empty);
    }

    private async void OnDataTimerTick(object? sender, EventArgs e)
    {
        try { await Update(); }
        catch (Exception ex) { Log($"Update error: {ex}"); }
    }

    private void ApplyTaskbarStates(IReadOnlyList<TaskbarState> states)
    {
        var seen = new HashSet<IntPtr>();

        foreach (var s in states)
        {
            if (!s.Found) continue;
            seen.Add(s.Hwnd);

            if (!_overlays.TryGetValue(s.Hwnd, out var overlay))
            {
                overlay = new OverlayWindow();
                overlay.PillClicked += TogglePopup;
                overlay.RightClicked += ShowContextMenu;
                overlay.SetTheme(_themeWatcher.IsLightTaskbar);
                _overlays[s.Hwnd] = overlay;
                if (_lastResult is not null) overlay.SetResult(_lastResult);
            }

            if (!s.TaskbarTopmost || s.AutoHidden)
            {
                if (overlay.IsVisible) overlay.Hide();
                continue;
            }

            if (!overlay.IsVisible) overlay.Show();
            overlay.UpdateLayout();

            var (left, top) = OverlayPositioner.Compute(
                s.Rect,
                overlay.ActualWidth,
                overlay.ActualHeight,
                s.DpiScale,
                _settings.PositionStrategy);

            overlay.Left = left;
            overlay.Top = top;
        }

        var stale = _overlays.Keys.Where(k => !seen.Contains(k)).ToList();
        foreach (var hwnd in stale)
        {
            try { _overlays[hwnd].Close(); } catch { /* already closing */ }
            _overlays.Remove(hwnd);
        }
    }

    private void ShowContextMenu(OverlayWindow source)
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

        ShowPopupAboveOverlay(source, _contextPopup);
    }

    private void TogglePopup(OverlayWindow source)
    {
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

        ShowPopupAboveOverlay(source, _popupWindow);
        _popupOpen = true;
    }

    private void ShowPopupAboveOverlay(OverlayWindow source, System.Windows.Window window)
    {
        window.Left = -10000;
        window.Top = -10000;
        window.Show();
        window.UpdateLayout();

        window.Left = source.Left;
        window.Top = source.Top - window.ActualHeight - 4;
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
            FanOutServerDown();
            return;
        }

        _requestInFlight = true;
        _lastFetchTime = DateTime.Now;

        try
        {
            var url = _settings.Url + $"?username={Environment.UserName}";
            var response = await _http.GetAsync(url).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                return;

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Log($"Fetch: {response.StatusCode}, contentLen={json.Length}");

            if (!_dumpedFirstResponse)
            {
                try { File.WriteAllText(Path.Combine(Path.GetDirectoryName(LogPath)!, "last-response.json"), json); } catch { }
                _dumpedFirstResponse = true;
            }

            var result = JsonSerializer.Deserialize<DevBarResult>(json);
            var totalItems = result?.Data.Values.Sum(l => l.Count) ?? -1;
            var nonEmpty = result?.Data.Count(kv => kv.Value.Count > 0) ?? 0;
            Log($"Parsed: categories={result?.Data.Count ?? -1}, nonEmpty={nonEmpty}, totalItems={totalItems}");

            if (result is not null)
            {
                var changed = ItemDiffer.GetChangedCategories(_lastResult, result);
                _lastResult = result;
                FanOutResult(result, changed);
            }
        }
        catch (Exception ex)
        {
            Log($"Fetch error: {ex.Message}");
            try { FanOutServerDown(); }
            catch (Exception ex2) { Log($"SetServerDown error: {ex2.Message}"); }
        }
        finally
        {
            _requestInFlight = false;
        }
    }

    private void ShowPreferences()
    {
        var window = new PreferencesWindow(_settings);
        window.SettingsSaved += newSettings =>
        {
            _settings = newSettings;
            var interval = Math.Max(_settings.RefreshSeconds, 0.1f);
            _dataTimer.Interval = TimeSpan.FromSeconds(interval);
        };
        window.Show();
        window.Activate();
    }

    private void OnThemeChanged()
    {
        var isLight = _themeWatcher.IsLightTaskbar;
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var overlay in _overlays.Values)
                overlay.SetTheme(isLight);
        });
        if (_lastResult is not null) FanOutResult(_lastResult, NoChanges);
        else FanOutServerDown();
    }

    private static readonly IReadOnlySet<string> NoChanges = new HashSet<string>();

    private void FanOutResult(DevBarResult result, IReadOnlySet<string> changedCategories)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var overlay in _overlays.Values)
                overlay.SetResult(result, changedCategories);
        });
    }

    private void FanOutServerDown()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var overlay in _overlays.Values)
                overlay.SetServerDown();
        });
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
            _dataTimer.Stop();
            _taskbarTimer.Stop();
            _hookListener.Dispose();
            _broadcast.Dispose();
            foreach (var overlay in _overlays.Values)
            {
                try { overlay.Close(); } catch { /* already closing */ }
            }
            _overlays.Clear();
            _http.Dispose();
        }
        base.Dispose(disposing);
    }
}
