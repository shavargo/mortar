namespace Mortar.Core.Rng;

public sealed class Pcg32
{
    private const ulong Multiplier = 6364136223846793005UL;
    private readonly ulong _increment;
    private ulong _state;

    public Pcg32(ulong initState, ulong initSequence)
    {
        _increment = (initSequence << 1) | 1UL;
        _state = 0;
        NextUInt();
        _state += initState;
        NextUInt();
    }

    public static Pcg32 ForAttempt(string areaId, uint seed, int attempt)
    {
        var mixed = Hashing.Fnv1a64(areaId) ^ (((ulong)seed << 32) | (uint)attempt);
        var state = Hashing.SplitMix64(mixed);
        return new Pcg32(state, Hashing.SplitMix64(state));
    }

    public uint NextUInt()
    {
        var old = _state;
        _state = old * Multiplier + _increment;
        var xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        var rotation = (int)(old >> 59);
        return (xorShifted >> rotation) | (xorShifted << (-rotation & 31));
    }

    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "must be positive");
        }
        var bound = (uint)maxExclusive;
        var threshold = (uint)((0x1_0000_0000UL - bound) % bound);
        while (true)
        {
            var value = NextUInt();
            if (value >= threshold)
            {
                return (int)(value % bound);
            }
        }
    }

    public int NextInt(int minInclusive, int maxExclusive) => minInclusive + NextInt(maxExclusive - minInclusive);

    public T Pick<T>(IReadOnlyList<T> items) => items[NextInt(items.Count)];

    public int PickWeighted(IReadOnlyList<int> weights)
    {
        var roll = NextInt(weights.Sum());
        for (var i = 0; i < weights.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0)
            {
                return i;
            }
        }
        return weights.Count - 1;
    }
}
