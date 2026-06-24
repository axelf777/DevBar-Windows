namespace DevBar;

public sealed class WinEventHookListener : IDisposable
{
    public event Action<IntPtr>? ForegroundChanged;

    private readonly IntPtr _hook;
    private readonly Win32Interop.WinEventDelegate _delegate;

    public WinEventHookListener()
    {
        _delegate = (_, _, hwnd, _, _, _, _) => ForegroundChanged?.Invoke(hwnd);
        _hook = Win32Interop.SetWinEventHook(
            Win32Interop.EVENT_SYSTEM_FOREGROUND, Win32Interop.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _delegate, 0, 0,
            Win32Interop.WINEVENT_OUTOFCONTEXT | Win32Interop.WINEVENT_SKIPOWNTHREAD);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero) Win32Interop.UnhookWinEvent(_hook);
    }
}
