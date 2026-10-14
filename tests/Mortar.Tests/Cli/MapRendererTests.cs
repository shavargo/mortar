using Mortar.Cli.Preview;

namespace Mortar.Tests.Cli;

public class MapRendererTests
{
    [Fact]
    public void GroundColor_UnknownGroundIsStable() =>
        Assert.Equal(MapRenderer.GroundColor("grass"), MapRenderer.GroundColor("grass"));

    [Fact]
    public void GroundColor_DifferentGroundsDiffer() =>
        Assert.NotEqual(MapRenderer.GroundColor("grass"), MapRenderer.GroundColor("road"));

    [Fact]
    public void GroundColor_KnownGroundUsesItsColor() =>
        Assert.Equal(new Rgb(196, 186, 166), MapRenderer.GroundColor("floor"));
}
