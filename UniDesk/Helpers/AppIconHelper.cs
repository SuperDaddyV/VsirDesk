using System.Drawing;
using System.Drawing.Drawing2D;
using DrawingColor = System.Drawing.Color;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UniDesk.Helpers;

public static class AppIconHelper
{
    private const string PackUri = "pack://application:,,,/icon/unidesk1-removebg-preview.ico";
    private const string RelativeFilePath = "icon/unidesk1-removebg-preview.ico";

    private static ImageSource? _windowIcon;
    private static Icon? _trayIcon;

    public static ImageSource? GetWindowIcon()
    {
        if (_windowIcon != null)
        {
            return _windowIcon;
        }

        try
        {
            var icon = new BitmapImage();
            icon.BeginInit();
            icon.UriSource = new Uri(PackUri, UriKind.Absolute);
            icon.CacheOption = BitmapCacheOption.OnLoad;
            icon.EndInit();
            icon.Freeze();
            _windowIcon = icon;
            return _windowIcon;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AppIconHelper.GetWindowIcon");
            return null;
        }
    }

    public static Icon? GetTrayIcon()
    {
        if (_trayIcon != null)
        {
            return (Icon)_trayIcon.Clone();
        }

        try
        {
            _trayIcon = LoadTrayIcon();
            if (_trayIcon != null)
            {
                return (Icon)_trayIcon.Clone();
            }

            using var source = LoadSourceBitmap();
            _trayIcon = source == null ? null : CreateTrayIcon(source);
            return _trayIcon == null ? null : (Icon)_trayIcon.Clone();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AppIconHelper.GetTrayIcon");
            return null;
        }
    }

    public static Icon CreateDefaultTrayIcon()
    {
        using var bitmap = new Bitmap(32, 32, DrawingPixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(DrawingColor.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(DrawingColor.FromArgb(0x6B, 0x9B, 0xD6));
        graphics.FillEllipse(brush, 2, 2, 28, 28);
        return CreateTrayIcon(bitmap) ?? Icon.ExtractAssociatedIcon(Environment.ProcessPath!)!;
    }

    public static void ApplyWindowIcon(Window window)
    {
        var icon = GetWindowIcon();
        if (icon != null)
        {
            window.Icon = icon;
        }
    }

    private static Icon? LoadTrayIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, RelativeFilePath);
        if (File.Exists(path))
        {
            return new Icon(path);
        }

        try
        {
            var resource = Application.GetResourceStream(new Uri(PackUri, UriKind.Absolute));
            return resource?.Stream == null ? null : new Icon(resource.Stream);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AppIconHelper.LoadTrayIcon");
            return null;
        }
    }

    private static Bitmap? LoadSourceBitmap()
    {
        var path = Path.Combine(AppContext.BaseDirectory, RelativeFilePath);
        if (File.Exists(path))
        {
            return new Bitmap(path);
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(PackUri, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            using var stream = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(stream);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AppIconHelper.LoadSourceBitmap");
            return null;
        }
    }

    private static Icon? CreateTrayIcon(Bitmap source)
    {
        var contentBounds = GetContentBounds(source);
        if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
        {
            contentBounds = new Rectangle(0, 0, source.Width, source.Height);
        }

        using var trayFrame = RenderTrayFrame(source, contentBounds, GetPreferredTrayIconSize());
        var handle = trayFrame.GetHicon();
        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static Bitmap RenderTrayFrame(Bitmap source, Rectangle contentBounds, int size)
    {
        var frame = new Bitmap(size, size, DrawingPixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(frame);
        graphics.Clear(DrawingColor.Transparent);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;

        const double fillRatio = 0.96;
        var target = (int)(size * fillRatio);
        var scale = Math.Min((float)target / contentBounds.Width, (float)target / contentBounds.Height);
        var drawWidth = Math.Max(1, (int)Math.Round(contentBounds.Width * scale));
        var drawHeight = Math.Max(1, (int)Math.Round(contentBounds.Height * scale));
        var x = (size - drawWidth) / 2;
        var y = (size - drawHeight) / 2;

        graphics.DrawImage(
            source,
            new Rectangle(x, y, drawWidth, drawHeight),
            contentBounds,
            GraphicsUnit.Pixel);

        return frame;
    }

    private static int GetPreferredTrayIconSize()
    {
        try
        {
            using var graphics = Graphics.FromHwnd(IntPtr.Zero);
            var dpi = graphics.DpiX;
            if (dpi >= 168)
            {
                return 32;
            }

            if (dpi >= 120)
            {
                return 24;
            }

            return 20;
        }
        catch
        {
            return 20;
        }
    }

    private static Rectangle GetContentBounds(Bitmap bitmap)
    {
        var minX = bitmap.Width;
        var minY = bitmap.Height;
        var maxX = 0;
        var maxY = 0;
        var found = false;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A <= 12)
                {
                    continue;
                }

                found = true;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (!found)
        {
            return Rectangle.Empty;
        }

        return Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);
}
