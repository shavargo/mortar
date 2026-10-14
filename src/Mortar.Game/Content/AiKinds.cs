namespace Mortar.Game.Content;

public static class AiKinds
{
    public const string MeleeChase = "melee-chase";
    public const string PackLeader = "pack-leader";

    public static bool IsKnown(string ai) => ai == MeleeChase || ai == PackLeader;
}
