using Mortar.Core.Rng;

namespace Mortar.Tests.Rng;

public class Pcg32Tests
{
    [Fact]
    public void MatchesPcgReferenceOutput()
    {
        var rng = new Pcg32(42, 54);
        uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
        foreach (var value in expected)
        {
            Assert.Equal(value, rng.NextUInt());
        }
    }

    [Fact]
    public void NextInt_StaysInRange()
    {
        var rng = new Pcg32(1, 1);
        for (var i = 0; i < 10_000; i++)
        {
            Assert.InRange(rng.NextInt(3, 7), 3, 6);
        }
    }

    [Fact]
    public void NextInt_RejectsEmptyRange()
    {
        var rng = new Pcg32(1, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
    }

    [Fact]
    public void ForAttempt_IsDeterministic()
    {
        var a = Pcg32.ForAttempt("crypt", 7, 0);
        var b = Pcg32.ForAttempt("crypt", 7, 0);
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(a.NextUInt(), b.NextUInt());
        }
    }

    [Fact]
    public void ForAttempt_DiffersByAreaSeedAndAttempt()
    {
        var baseline = Pcg32.ForAttempt("crypt", 7, 0).NextUInt();
        Assert.NotEqual(baseline, Pcg32.ForAttempt("crypt", 7, 1).NextUInt());
        Assert.NotEqual(baseline, Pcg32.ForAttempt("crypt", 8, 0).NextUInt());
        Assert.NotEqual(baseline, Pcg32.ForAttempt("forest", 7, 0).NextUInt());
    }

    [Fact]
    public void PickWeighted_NeverPicksZeroWeights()
    {
        var rng = new Pcg32(3, 3);
        for (var i = 0; i < 1_000; i++)
        {
            Assert.Equal(1, rng.PickWeighted(new[] { 0, 5, 0 }));
        }
    }
}
