using Mortar.Core.Layouts;

namespace Mortar.Core.Indoor;

internal sealed class BuildOutcome
{
    private BuildOutcome(CellGrid? grid, IReadOnlyList<Marker> markers, string? failure)
    {
        Grid = grid;
        Markers = markers;
        Failure = failure;
    }

    public CellGrid? Grid { get; }

    public IReadOnlyList<Marker> Markers { get; }

    public string? Failure { get; }

    public static BuildOutcome Built(CellGrid grid, IReadOnlyList<Marker> markers) =>
        new BuildOutcome(grid, markers, null);

    public static BuildOutcome Failed(string failure) =>
        new BuildOutcome(null, Array.Empty<Marker>(), failure);
}
