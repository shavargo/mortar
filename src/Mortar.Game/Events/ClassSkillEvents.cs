namespace Mortar.Game.Events;

public sealed class StaffRecharged : GameEvent
{
    public StaffRecharged(long tick, int itemId, int charges, int manaSpent)
        : base(tick)
    {
        ItemId = itemId;
        Charges = charges;
        ManaSpent = manaSpent;
    }

    public int ItemId { get; }

    public int Charges { get; }

    public int ManaSpent { get; }

    protected override string Describe() => $"recharged staff #{ItemId} to {Charges} (spent {ManaSpent} mana)";
}
