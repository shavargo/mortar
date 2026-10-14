using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Mortar.Cli.Preview;

namespace Mortar.Tests.Cli;

public class PngEncoderTests
{
    [Fact]
    public void Crc32_MatchesReferenceVector() =>
        Assert.Equal(0xCBF43926u, Crc32.Compute(Encoding.ASCII.GetBytes("123456789")));

    [Fact]
    public void EncodeRgb_ProducesDecodableImage()
    {
        var png = PngEncoder.EncodeRgb(2, 1, new byte[] { 255, 0, 0, 0, 0, 255 });
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
        Assert.Equal("IHDR", Encoding.ASCII.GetString(png, 12, 4));
        Assert.Equal(2, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)));
        Assert.Equal(1, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
        Assert.Equal("IDAT", Encoding.ASCII.GetString(png, 37, 4));
        var idatLength = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(33));
        using var inflater = new ZLibStream(new MemoryStream(png, 41, idatLength), CompressionMode.Decompress);
        using var raw = new MemoryStream();
        inflater.CopyTo(raw);
        Assert.Equal(new byte[] { 0, 255, 0, 0, 0, 0, 255 }, raw.ToArray());
    }
}
