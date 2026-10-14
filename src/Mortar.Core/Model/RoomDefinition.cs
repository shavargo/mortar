namespace Mortar.Core.Model;

public sealed class RoomDefinition
{
    public RoomDefinition(string source, string id, IReadOnlyList<string> tags, IReadOnlyList<string> grid)
    {
        Source = source;
        Id = id;
        Tags = tags;
        Grid = grid;
    }

    public string Source { get; }

    public string Id { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> Grid { get; }
}
