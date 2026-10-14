using Mortar.Core.Generation;
using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Generation;

public class ContentAreaTests
{
    private const uint SeedCount = 500;

    public static TheoryData<string> CathedralAreas => new() { "cathedral-1", "cathedral-2", "cathedral-3", "cathedral-4" };

    private static CompiledArea Load(string name)
    {
        var result = MortarLoader.LoadArea(TestPaths.ContentArea(name));
        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
        return result.Value!;
    }

    [Theory]
    [MemberData(nameof(CathedralAreas))]
    public void Cathedral_GeneratesOnEverySeed(string name)
    {
        var area = Load(name);
        var failures = new List<string>();
        for (var seed = 1u; seed <= SeedCount; seed++)
        {
            var result = Generator.Generate(area, seed);
            if (!result.Succeeded) failures.Add($"seed {seed}: {result.FailureSummary}");
        }
        Assert.Empty(failures);
    }

    [Theory]
    [MemberData(nameof(CathedralAreas))]
    public void Cathedral_MatchesSnapshot(string name)
    {
        var area = Load(name);
        var result = Generator.Generate(area, 1);
        Assert.True(result.Succeeded, result.FailureSummary);
        Snapshots.AssertMatches($"{name}-seed-1.txt", AsciiMap.Render(result.Layout!, area.Tileset));
    }

    [Fact]
    public void Cathedral_UsesMultiSlotFillers()
    {
        var area = Load("cathedral-3");
        var found = false;
        for (var seed = 1u; seed <= 20 && !found; seed++)
        {
            var grid = SlotGrid.ForArea(area.Definition);
            var rng = Pcg32.ForAttempt(area.Definition.Id, seed, 0);
            var failure = LandmarkPlacer.PlaceAll(area, grid, rng) ?? IndoorRouter.Route(area, grid, rng) ?? RoomFiller.FillAll(area, grid, rng);
            found = failure is null && grid.Placements.Any(p => p.NodeId is null && !p.Room.IsSingleSlot);
        }
        Assert.True(found, "no multi-slot filler was placed in 20 seeds");
    }
}
