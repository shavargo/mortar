namespace Mortar.Game.Content;

public static class ClassSkills
{
    public const string Disarm = "disarm";
    public const string Recharge = "recharge";

    public static bool IsKnown(string skill) => skill == Disarm || skill == Recharge;
}
