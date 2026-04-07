using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace DevBar;

public enum IconState { AllClear, HasItems, HighPriority, ServerDown }

public static partial class IconRenderer
{
    private static readonly Color Green = ColorTranslator.FromHtml("#4CAF50");
    private static readonly Color Amber = ColorTranslator.FromHtml("#FF9800");
    private static readonly Color Red = ColorTranslator.FromHtml("#F44336");
    private static readonly Color Gray = ColorTranslator.FromHtml("#9E9E9E");

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr handle);

    public static Icon Create(IconState state)
    {
        const int size = 64;
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var color = state switch
        {
            IconState.AllClear => Green,
            IconState.HasItems => Amber,
            IconState.HighPriority => Red,
            _ => Gray
        };

        // Fill the entire icon area — no padding
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, size - 2, size - 2);

        using var pen = new Pen(Color.White, 5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        switch (state)
        {
            case IconState.AllClear:
                g.DrawLine(pen, 18, 34, 27, 43);
                g.DrawLine(pen, 27, 43, 46, 22);
                break;
            case IconState.HasItems:
                // Dot in center for "items exist"
                g.FillEllipse(Brushes.White, 26, 26, 12, 12);
                break;
            case IconState.HighPriority:
                g.DrawLine(pen, 32, 16, 32, 36);
                g.FillEllipse(Brushes.White, 28, 42, 8, 8);
                break;
            case IconState.ServerDown:
                g.DrawArc(pen, 20, 14, 24, 22, 180, 230);
                g.DrawLine(pen, 32, 34, 32, 36);
                g.FillEllipse(Brushes.White, 28, 42, 8, 8);
                break;
        }

        var hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    /// <summary>
    /// Saves a PNG app logo for use in toast notifications and window icons.
    /// Returns the file path.
    /// </summary>
    public static string SaveAppLogoPng()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevBar");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "devbar-logo.png");

        if (File.Exists(path)) return path;

        const int size = 128;
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // Dark background circle with a green "D"
        using var bgBrush = new SolidBrush(ColorTranslator.FromHtml("#2D2D30"));
        g.FillEllipse(bgBrush, 0, 0, size, size);

        using var border = new Pen(Green, 4f);
        g.DrawEllipse(border, 2, 2, size - 4, size - 4);

        using var font = new Font("Segoe UI", 56, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Green);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("D", font, textBrush, new RectangleF(0, 0, size, size), sf);

        bmp.Save(path, ImageFormat.Png);
        return path;
    }
}
