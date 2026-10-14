using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Mortar.Core.Rng;

namespace Mortar.Core.Layouts;

public static class LayoutFingerprint
{
    public static string Compute(string areaId, uint seed, int attempt, LayoutCells cells)
    {
        var hash = Hashing.Fnv1a64(TerminatedUtf8(areaId));
        hash = AddUInt32(hash, seed);
        hash = AddUInt32(hash, (uint)attempt);
        hash = AddUInt32(hash, (uint)cells.Width);
        hash = AddUInt32(hash, (uint)cells.Height);
        var tileBytes = cells.TileTypes.Select(TerminatedUtf8).ToArray();
        for (var i = 0; i < cells.Tiles.Count; i++)
        {
            hash = Hashing.Fnv1a64(tileBytes[cells.Tiles[i]], hash);
            hash = AddUInt32(hash, (uint)cells.Variants[i]);
        }
        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    private static byte[] TerminatedUtf8(string text)
    {
        var bytes = new byte[Encoding.UTF8.GetByteCount(text) + 1];
        Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);
        return bytes;
    }

    private static ulong AddUInt32(ulong hash, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return Hashing.Fnv1a64(bytes, hash);
    }
}
