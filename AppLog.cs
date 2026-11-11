using System.IO;

namespace DevBar;

/// <summary>
/// Size-capped log at %AppData%\DevBar\devbar.log. Once the file reaches
/// <see cref="MaxBytes"/> it is moved to devbar.log.1 (replacing the previous
/// one), so the log never uses more than about twice that on disk.
/// </summary>
public static class AppLog
{
    private const long MaxBytes = 1024 * 1024;

    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevBar");

    private static readonly string LogPath = Path.Combine(LogDirectory, "devbar.log");
    private static readonly object WriteLock = new();

    public static void Write(string message)
    {
        lock (WriteLock)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                RotateIfFull();
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch { /* logging must never crash the app */ }
        }
    }

    private static void RotateIfFull()
    {
        var log = new FileInfo(LogPath);
        if (log.Exists && log.Length >= MaxBytes)
            File.Move(LogPath, LogPath + ".1", overwrite: true);
    }
}
