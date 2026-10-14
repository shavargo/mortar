namespace Mortar.Game.Content;

public static class ItemSlots
{
    public const string Weapon = "weapon";
    public const string Body = "body";
    public const string Head = "head";
    public const string Shield = "shield";
    public const string Ring = "ring";
    public const string Amulet = "amulet";
    public const string Potion = "potion";

    public static readonly IReadOnlyList<string> All = new[] { Weapon, Body, Head, Shield, Ring, Amulet, Potion };

    public static readonly IReadOnlyList<string> Equippable = new[] { Weapon, Body, Head, Shield, Ring, Amulet };

    public static bool IsKnown(string slot) => All.Contains(slot);

    public static bool IsEquippable(string slot) => Equippable.Contains(slot);
}
