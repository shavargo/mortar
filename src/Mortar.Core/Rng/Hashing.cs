using System.Text;

namespace Mortar.Core.Rng;

public static class Hashing
{
    public const ulong FnvOffset = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    public static ulong Fnv1a64(ReadOnlySpan<byte> data, ulong hash = FnvOffset)
    {
        foreach (var value in data)
        {
            hash ^= value;
            hash *= FnvPrime;
        }
        return hash;
    }

    public static ulong Fnv1a64(string text) => Fnv1a64(Encoding.UTF8.GetBytes(text));

    public static ulong SplitMix64(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
