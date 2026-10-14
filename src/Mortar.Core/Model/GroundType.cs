namespace Mortar.Core.Model;

public sealed class GroundType
{
    public GroundType(string id, char symbol, bool walkable, bool blocksSight)
    {
        Id = id;
        Symbol = symbol;
        Walkable = walkable;
        BlocksSight = blocksSight;
    }

    public string Id { get; }

    public char Symbol { get; }

    public bool Walkable { get; }

    public bool BlocksSight { get; }
}
