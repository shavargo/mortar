namespace Mortar.Game.Events;

public sealed class VendorSessionEnded : GameEvent
{
    public VendorSessionEnded(long tick, string vendorKind)
        : base(tick)
    {
        VendorKind = vendorKind;
    }

    public string VendorKind { get; }

    protected override string Describe() => $"vendor session ended ({VendorKind})";
}

public sealed class VendorStockRefreshed : GameEvent
{
    public VendorStockRefreshed(long tick)
        : base(tick)
    {
    }

    protected override string Describe() => "vendor stock refreshed";
}
