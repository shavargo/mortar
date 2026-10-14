using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Mortar.Cli.Preview;

internal static class PngEncoder
{
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static byte[] EncodeRgb(int width, int height, byte[] rgb)
    {
        using var output = new MemoryStream();
        output.Write(Signature);
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 2;
        WriteChunk(output, "IHDR", header);
        WriteChunk(output, "IDAT", Compress(width, height, rgb));
        WriteChunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    private static byte[] Compress(int width, int height, byte[] rgb)
    {
        using var buffer = new MemoryStream();
        using (var zlib = new ZLibStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
        {
            var stride = width * 3;
            for (var y = 0; y < height; y++)
            {
                zlib.WriteByte(0);
                zlib.Write(rgb, y * stride, stride);
            }
        }
        return buffer.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        output.Write(number);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(number, Crc32.Compute(typeBytes, data));
        output.Write(number);
    }
}
