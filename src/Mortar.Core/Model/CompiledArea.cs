namespace Mortar.Core.Model;

public sealed class CompiledArea
{
    internal CompiledArea(AreaDefinition definition, Tileset tileset, IReadOnlyList<Room> rooms)
    {
        Definition = definition;
        Tileset = tileset;
        Rooms = rooms;
    }

    public AreaDefinition Definition { get; }

    public Tileset Tileset { get; }

    public IReadOnlyList<Room> Rooms { get; }

    public IReadOnlyList<Room> RoomsTagged(string tag) => Rooms.Where(r => r.HasTag(tag)).ToList();

    public GraphNode Node(string id) => Definition.Nodes.First(n => n.Id == id);
}
