using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace DevBar;

public static class IconRenderer
{
    private static readonly Color Green = ColorTranslator.FromHtml("#4CAF50");

    /// <summary>
    /// Saves a PNG app logo for use as the Preferences window icon. Returns the file path.
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
