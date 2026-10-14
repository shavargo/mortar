namespace Mortar.Core.Layouts;

public sealed class Layout
{
    public const int CurrentFormatVersion = 1;
    public const int MaxDimension = 4096;

    public Layout(
        string areaId,
        uint seed,
        int attempt,
        LayoutCells cells,
        IReadOnlyList<Marker> markers,
        string mortarVersion,
        int formatVersion)
    {
        AreaId = areaId;
        Seed = seed;
        Attempt = attempt;
        Cells = cells;
        Markers = markers;
        MortarVersion = mortarVersion;
        FormatVersion = formatVersion;
        Fingerprint = LayoutFingerprint.Compute(areaId, seed, attempt, cells);
    }

    public string AreaId { get; }

    public uint Seed { get; }

    public int Attempt { get; }

    public int Width => Cells.Width;

    public int Height => Cells.Height;

    public LayoutCells Cells { get; }

    public IReadOnlyList<Marker> Markers { get; }

    public string MortarVersion { get; }

    public int FormatVersion { get; }

    public string Fingerprint { get; }
}
