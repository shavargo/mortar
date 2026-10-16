namespace Mortar.Game.Commands;

public sealed class UseItemCommand : Command
{
    public UseItemCommand(int itemId, int targetActorId = 0, Cell? targetCell = null, int targetItemId = 0)
    {
        ItemId = itemId;
        TargetActorId = targetActorId;
        TargetCell = targetCell;
        TargetItemId = targetItemId;
    }

    public int ItemId { get; }

    public int TargetActorId { get; }

    public Cell? TargetCell { get; }

    public int TargetItemId { get; }

    public override string Describe() => $"use item #{ItemId}";
}
