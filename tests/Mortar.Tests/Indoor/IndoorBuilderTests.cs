using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class IndoorBuilderTests
{
    [Fact]
    public void Build_SucceedsForMostSeeds()
    {
        var area = TestContent.CompileCrypt();
        var outcomes = Enumerable.Range(0, 20)
            .Select(seed => IndoorBuilder.Build(area, Pcg32.ForAttempt("crypt", (uint)seed, 0)))
            .ToList();
        Assert.True(outcomes.Count(o => o.Failure is null) >= 16);
        Assert.All(outcomes.Where(o => o.Failure is null), outcome =>
        {
            Assert.Equal(57, outcome.Grid!.Width);
            Assert.Equal(MarkerKinds.Entrance, outcome.Markers[0].Kind);
        });
    }

    [Fact]
    public void Build_IsDeterministic()
    {
        var area = TestContent.CompileCrypt();
        var first = IndoorBuilder.Build(area, Pcg32.ForAttempt("crypt", 5, 0));
        var second = IndoorBuilder.Build(area, Pcg32.ForAttempt("crypt", 5, 0));
        Assert.Equal(first.Failure, second.Failure);
        Assert.Equal(first.Grid?.Ground, second.Grid?.Ground);
    }
}
