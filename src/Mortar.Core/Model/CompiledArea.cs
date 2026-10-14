namespace Mortar.Core.Model;

public sealed class CompiledArea
{
    internal CompiledArea(AreaDefinition definition, Tileset tileset, IReadOnlyList<Room> rooms)
        : this(definition, tileset, rooms, null)
    {
    }

    internal CompiledArea(AreaDefinition definition, Tileset tileset, FixedMap map)
        : this(definition, tileset, Array.Empty<Room>(), map)
    {
    }

    private CompiledArea(AreaDefinition definition, Tileset tileset, IReadOnlyList<Room> rooms, FixedMap? map)
    {
        Definition = definition;
        Tileset = tileset;
        Rooms = rooms;
        Map = map;
    }

    public AreaDefinition Definition { get; }

    public Tileset Tileset { get; }

    public IReadOnlyList<Room> Rooms { get; }

    public FixedMap? Map { get; }

    public IReadOnlyList<Room> RoomsTagged(string tag) => Rooms.Where(r => r.HasTag(tag)).ToList();

    public GraphNode Node(string id) => Definition.Nodes.First(n => n.Id == id);
}
