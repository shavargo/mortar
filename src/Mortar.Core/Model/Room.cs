namespace Mortar.Core.Model;

public sealed class Room
{
    public const int MaxWeight = 1000;

    private readonly int[] _ground;

    internal Room(
        string id,
        IReadOnlyList<string> tags,
        int weight,
        int width,
        int height,
        int stride,
        int[] ground,
        IReadOnlyList<Door> doors,
        IReadOnlyList<RoomMarker> markers)
    {
        Id = id;
        Tags = tags;
        Weight = weight;
        Width = width;
        Height = height;
        Stride = stride;
        _ground = ground;
        Doors = doors;
        Markers = markers;
    }

    public string Id { get; }

    public IReadOnlyList<string> Tags { get; }

    public int Weight { get; }

    public int Width { get; }

    public int Height { get; }

    public int Stride { get; }

    public int SlotsWide => (Width - 1) / Stride;

    public int SlotsHigh => (Height - 1) / Stride;

    public bool IsSingleSlot => SlotsWide == 1 && SlotsHigh == 1;

    public IReadOnlyList<Door> Doors { get; }

    public IReadOnlyList<RoomMarker> Markers { get; }

    public int GroundAt(int x, int y) => _ground[y * Width + x];

    public bool HasTag(string tag) => Tags.Contains(tag);

    public bool HasDoorAt(int x, int y) => Doors.Any(d => d.X == x && d.Y == y);

    public bool HasDoorInSegment(Side side, int segment) => Doors.Any(d => d.Side == side && d.Segment == segment);
}
