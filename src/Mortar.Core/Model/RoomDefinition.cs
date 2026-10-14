namespace Mortar.Core.Model;

public sealed class RoomDefinition
{
    public RoomDefinition(string source, string id, IReadOnlyList<string> tags, IReadOnlyList<string> grid, int weight = 1)
    {
        Source = source;
        Id = id;
        Tags = tags;
        Grid = grid;
        Weight = weight;
    }

    public string Source { get; }

    public string Id { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> Grid { get; }

    public int Weight { get; }
}
