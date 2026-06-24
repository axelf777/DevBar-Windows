using System.Windows.Interop;

namespace DevBar;

public sealed class BroadcastListener : IDisposable
{
    public event Action? TaskbarCreated;

    private readonly HwndSource _source;
    private readonly uint _taskbarCreatedMsg;

    public BroadcastListener()
    {
        _taskbarCreatedMsg = Win32Interop.RegisterWindowMessage("TaskbarCreated");

        var parameters = new HwndSourceParameters("DevBar.BroadcastListener")
        {
            ParentWindow = (IntPtr)Win32Interop.HWND_MESSAGE,
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if ((uint)msg == _taskbarCreatedMsg)
            TaskbarCreated?.Invoke();
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
