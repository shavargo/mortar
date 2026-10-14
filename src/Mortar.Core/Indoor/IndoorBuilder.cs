using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class IndoorBuilder
{
    public static BuildOutcome Build(CompiledArea area, Pcg32 rng)
    {
        var grid = SlotGrid.ForArea(area.Definition);
        var failure = LandmarkPlacer.PlaceAll(area, grid, rng)
            ?? IndoorRouter.Route(area, grid, rng)
            ?? RoomFiller.FillAll(area, grid, rng);
        if (failure is not null)
        {
            return BuildOutcome.Failed(failure);
        }
        return BuildOutcome.Built(RoomStitcher.Stitch(grid, area.Tileset), MarkerCollector.Collect(area, grid));
    }
}
