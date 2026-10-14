namespace Mortar.Game.Commands;

public sealed class PickUpCommand : Command
{
    public PickUpCommand(int groundItemId)
    {
        GroundItemId = groundItemId;
    }

    public int GroundItemId { get; }

    public override string Describe() => $"pick up item {GroundItemId}";
}
