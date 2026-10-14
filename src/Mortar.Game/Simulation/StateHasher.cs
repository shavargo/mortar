using System.Buffers.Binary;
using System.Text;
using Mortar.Core.Rng;

namespace Mortar.Game.Simulation;

internal sealed class StateHasher
{
    public ulong Value { get; private set; } = Hashing.FnvOffset;

    public StateHasher Add(long value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        Value = Hashing.Fnv1a64(bytes, Value);
        return this;
    }

    public StateHasher Add(ulong value) => Add(unchecked((long)value));

    public StateHasher Add(uint value) => Add((long)value);

    public StateHasher Add(bool value) => Add(value ? 1L : 0L);

    public StateHasher Add(string value)
    {
        Add((long)value.Length);
        Value = Hashing.Fnv1a64(Encoding.UTF8.GetBytes(value), Value);
        return this;
    }
}
