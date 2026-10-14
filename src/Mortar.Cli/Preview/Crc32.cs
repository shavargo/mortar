namespace Mortar.Cli.Preview;

internal static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    public static uint Compute(params byte[][] parts)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var part in parts)
        {
            foreach (var value in part)
            {
                crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
            }
        }
        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }
            table[n] = c;
        }
        return table;
    }
}
