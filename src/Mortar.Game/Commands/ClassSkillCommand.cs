namespace Mortar.Game.Commands;

public sealed class ClassSkillCommand : Command
{
    public ClassSkillCommand(int? itemId = null)
    {
        ItemId = itemId;
    }

    public int? ItemId { get; }

    public override string Describe() =>
        ItemId is { } id ? $"class skill on item #{id}" : "class skill";
}
