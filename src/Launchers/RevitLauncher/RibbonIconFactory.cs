using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfColor = System.Windows.Media.Color;

namespace BIMCapabilities.Launchers.Revit;

/// <summary>
/// Builds ribbon icons: document + compliance check (Revit add-in style).
/// </summary>
internal static class RibbonIconFactory
{
    private static readonly WpfColor DocumentBody = WpfColor.FromRgb(248, 250, 252);
    private static readonly WpfColor DocumentHeader = WpfColor.FromRgb(43, 87, 154);
    private static readonly WpfColor DocumentBorder = WpfColor.FromRgb(180, 195, 215);
    private static readonly WpfColor CheckGreen = WpfColor.FromRgb(39, 127, 78);
    private static readonly WpfColor CheckCircle = WpfColor.FromRgb(232, 245, 237);

    internal static BitmapSource CreateLargeImage() => CreateImage(32);

    internal static BitmapSource CreateSmallImage() => CreateImage(16);

    private static BitmapSource CreateImage(int size)
    {
        var pixels = new byte[size * size * 4];
        var scale = size / 32.0;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                SetPixel(pixels, size, x, y, GetPixelColor(x, y, scale));
            }
        }

        var stride = size * 4;
        return BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
    }

    private static WpfColor GetPixelColor(int x, int y, double scale)
    {
        var sx = x / scale;
        var sy = y / scale;

        if (IsInCheckBadge(sx, sy) && IsCheckMark(sx, sy))
        {
            return CheckGreen;
        }

        if (IsInCheckBadge(sx, sy))
        {
            return CheckCircle;
        }

        if (IsInDocumentHeader(sx, sy))
        {
            return DocumentHeader;
        }

        if (IsInDocumentBody(sx, sy))
        {
            return IsOnDocumentLine(sx, sy) ? DocumentBorder : DocumentBody;
        }

        if (IsOnDocumentBorder(sx, sy))
        {
            return DocumentBorder;
        }

        return Colors.Transparent;
    }

    private static bool IsInDocumentBody(double x, double y)
    {
        return x >= 5 && x <= 22 && y >= 7 && y <= 27;
    }

    private static bool IsInDocumentHeader(double x, double y)
    {
        return x >= 5 && x <= 22 && y >= 7 && y <= 11;
    }

    private static bool IsOnDocumentBorder(double x, double y)
    {
        var onLeft = x >= 4 && x < 5 && y >= 6 && y <= 28;
        var onRight = x > 22 && x <= 23 && y >= 6 && y <= 28;
        var onTop = y >= 6 && y < 7 && x >= 4 && x <= 23;
        var onBottom = y > 27 && y <= 28 && x >= 4 && x <= 23;
        return onLeft || onRight || onTop || onBottom;
    }

    private static bool IsOnDocumentLine(double x, double y)
    {
        if (y >= 14 && y <= 14.8 && x >= 8 && x <= 19)
        {
            return true;
        }

        if (y >= 18 && y <= 18.8 && x >= 8 && x <= 17)
        {
            return true;
        }

        return y >= 22 && y <= 22.8 && x >= 8 && x <= 18;
    }

    private static bool IsInCheckBadge(double x, double y)
    {
        var centerX = 24.5;
        var centerY = 24.5;
        var radius = 6.5;
        var dx = x - centerX;
        var dy = y - centerY;
        return dx * dx + dy * dy <= radius * radius;
    }

    private static bool IsCheckMark(double x, double y)
    {
        var onStem = x >= 21.5 && x <= 22.8 && y >= 22 && y <= 25.5 && x - y * 0.55 >= 8;
        var onArm = x >= 22.5 && x <= 27.5 && y >= 21 && y <= 23.2 && x + y * 0.9 >= 44;
        return onStem || onArm;
    }

    private static void SetPixel(byte[] pixels, int size, int x, int y, WpfColor color)
    {
        var offset = (y * size + x) * 4;
        pixels[offset] = color.B;
        pixels[offset + 1] = color.G;
        pixels[offset + 2] = color.R;
        pixels[offset + 3] = color.A;
    }
}
