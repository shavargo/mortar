namespace Mortar.Cli.Preview;

internal readonly record struct Rgb(byte R, byte G, byte B);

internal sealed class RgbImage
{
    public RgbImage(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new byte[width * height * 3];
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Pixels { get; }

    public void FillRect(int x, int y, int width, int height, Rgb color)
    {
        for (var py = Math.Max(0, y); py < Math.Min(Height, y + height); py++)
        {
            for (var px = Math.Max(0, x); px < Math.Min(Width, x + width); px++)
            {
                var index = (py * Width + px) * 3;
                Pixels[index] = color.R;
                Pixels[index + 1] = color.G;
                Pixels[index + 2] = color.B;
            }
        }
    }
}
