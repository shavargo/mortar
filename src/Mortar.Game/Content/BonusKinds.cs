namespace Mortar.Game.Content;

public static class BonusKinds
{
    public const string Strength = "strength";
    public const string Dexterity = "dexterity";
    public const string Vitality = "vitality";
    public const string Magic = "magic";
    public const string Life = "life";
    public const string Mana = "mana";
    public const string ArmorClass = "armorClass";
    public const string ToHit = "toHit";
    public const string Damage = "damage";

    public static readonly IReadOnlyList<string> All =
        new[] { Strength, Dexterity, Vitality, Magic, Life, Mana, ArmorClass, ToHit, Damage };

    public static bool IsKnown(string kind) => All.Contains(kind);
}
