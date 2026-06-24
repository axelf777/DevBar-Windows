namespace DevBar;

public readonly record struct TaskbarState(
    IntPtr Hwnd,
    bool Found,
    RECT Rect,
    bool TaskbarTopmost,
    bool AutoHidden,
    double DpiScale);

public sealed class TaskbarTracker
{
    public event Action<IReadOnlyList<TaskbarState>>? StateChanged;

    private List<TaskbarState> _last = new();

    public void Tick()
    {
        var states = ReadAll();
        if (StatesEqual(states, _last)) return;
        _last = states;
        StateChanged?.Invoke(states);
    }

    private static List<TaskbarState> ReadAll()
    {
        var result = new List<TaskbarState>();

        var primary = Win32Interop.FindWindow("Shell_TrayWnd", null);
        if (primary != IntPtr.Zero)
        {
            var s = Read(primary);
            if (s.Found) result.Add(s);
        }

        var child = IntPtr.Zero;
        while (true)
        {
            child = Win32Interop.FindWindowEx(IntPtr.Zero, child, "Shell_SecondaryTrayWnd", null);
            if (child == IntPtr.Zero) break;
            var s = Read(child);
            if (s.Found) result.Add(s);
        }

        return result;
    }

    private static TaskbarState Read(IntPtr hwnd)
    {
        if (!Win32Interop.GetWindowRect(hwnd, out var rect))
            return new TaskbarState(hwnd, false, default, false, false, 1.0);

        var ex = Win32Interop.GetWindowLong(hwnd, Win32Interop.GWL_EXSTYLE);
        var topmost = (ex & Win32Interop.WS_EX_TOPMOST) != 0;

        var autoHidden = IsAutoHiddenAndCollapsed(rect);
        var dpi = GetDpiScale(hwnd);

        return new TaskbarState(hwnd, true, rect, topmost, autoHidden, dpi);
    }

    private static bool StatesEqual(List<TaskbarState> a, List<TaskbarState> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (!a[i].Equals(b[i])) return false;
        return true;
    }

    private static bool IsAutoHiddenAndCollapsed(RECT taskbarRect)
    {
        var data = new APPBARDATA { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<APPBARDATA>() };
        var state = (int)Win32Interop.SHAppBarMessage(Win32Interop.ABM_GETSTATE, ref data);
        if ((state & Win32Interop.ABS_AUTOHIDE) == 0) return false;

        // When auto-hidden and collapsed, the bar slides off-screen so only ~2 px peek out.
        var screenBottom = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Bottom ?? 0;
        return taskbarRect.Top >= screenBottom - 4;
    }

    private static double GetDpiScale(IntPtr hwnd)
    {
        var mon = Win32Interop.MonitorFromWindow(hwnd, Win32Interop.MONITOR_DEFAULTTONEAREST);
        if (mon == IntPtr.Zero) return 1.0;
        if (Win32Interop.GetDpiForMonitor(mon, Win32Interop.MDT_EFFECTIVE_DPI, out var dx, out _) != 0)
            return 1.0;
        return dx / 96.0;
    }
}
