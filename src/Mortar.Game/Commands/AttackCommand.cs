namespace Mortar.Game.Commands;

public sealed class AttackCommand : Command
{
    public AttackCommand(int targetId)
    {
        TargetId = targetId;
    }

    public int TargetId { get; }

    public override string Describe() => $"attack #{TargetId}";
}
