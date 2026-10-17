using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

public sealed class VendorState
{
    private readonly List<Item> _stock = new List<Item>();

    public VendorState(string kind)
    {
        Kind = kind;
    }

    public string Kind { get; }

    public IReadOnlyList<Item> Stock => _stock;

    internal List<Item> MutableStock => _stock;
}
