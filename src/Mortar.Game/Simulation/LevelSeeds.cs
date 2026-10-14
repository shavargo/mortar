using Mortar.Core.Rng;

namespace Mortar.Game.Simulation;

public static class LevelSeeds
{
    public static uint For(uint worldSeed, int depth) =>
        (uint)(Hashing.SplitMix64(((ulong)worldSeed << 16) ^ (uint)depth) >> 32);
}
