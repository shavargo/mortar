namespace Mortar.Game.Commands;

public sealed class CastCommand : Command
{
    public CastCommand(string spellId, int targetActorId = 0, Cell? targetCell = null)
    {
        SpellId = spellId;
        TargetActorId = targetActorId;
        TargetCell = targetCell;
    }

    public string SpellId { get; }

    public int TargetActorId { get; }

    public Cell? TargetCell { get; }

    public override string Describe() =>
        TargetActorId != 0 ? $"cast {SpellId} at #{TargetActorId}"
        : TargetCell is { } cell ? $"cast {SpellId} at {cell}"
        : $"cast {SpellId}";
}
