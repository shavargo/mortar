using Mortar.Core.Rng;

namespace Mortar.Tests.Rng;

public class HashingTests
{
    [Theory]
    [InlineData("", 0xcbf29ce484222325UL)]
    [InlineData("a", 0xaf63dc4c8601ec8cUL)]
    [InlineData("foobar", 0x85944171f73967e8UL)]
    public void Fnv1a64_MatchesReferenceVectors(string text, ulong expected)
    {
        Assert.Equal(expected, Hashing.Fnv1a64(text));
    }

    [Fact]
    public void Fnv1a64_ContinuesFromAPreviousHash()
    {
        var partial = Hashing.Fnv1a64("foo");
        var continued = Hashing.Fnv1a64(System.Text.Encoding.UTF8.GetBytes("bar"), partial);
        Assert.Equal(Hashing.Fnv1a64("foobar"), continued);
    }

    [Fact]
    public void SplitMix64_MatchesReferenceVector()
    {
        Assert.Equal(0xe220a8397b1dcdafUL, Hashing.SplitMix64(0));
    }
}
