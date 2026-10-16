namespace Mortar.Game.Commands;

public sealed class CastCommand : Command
{
    public CastCommand(string spellId, int targetActorId = 0, Cell? targetCell = null, int itemId = 0)
    {
        SpellId = spellId;
        TargetActorId = targetActorId;
        TargetCell = targetCell;
        ItemId = itemId;
    }

    public string SpellId { get; }

    public int TargetActorId { get; }

    public Cell? TargetCell { get; }

    public int ItemId { get; }

    public override string Describe() =>
        ItemId != 0 ? $"cast {SpellId} on item #{ItemId}"
        : TargetActorId != 0 ? $"cast {SpellId} at #{TargetActorId}"
        : TargetCell is { } cell ? $"cast {SpellId} at {cell}"
        : $"cast {SpellId}";
}
