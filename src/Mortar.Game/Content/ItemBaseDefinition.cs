namespace Mortar.Game.Content;

public sealed class WeaponSwing
{
    public WeaponSwing(int attackTicks, int hitFrame)
    {
        AttackTicks = attackTicks;
        HitFrame = hitFrame;
    }

    public int AttackTicks { get; }

    public int HitFrame { get; }
}

public static class PotionKinds
{
    public const string Life = "life";
    public const string Mana = "mana";

    public static bool IsKnown(string kind) => kind == Life || kind == Mana;
}

public sealed class PotionEffect
{
    public PotionEffect(string restores, int amount)
    {
        Restores = restores;
        Amount = amount;
    }

    public string Restores { get; }

    public int Amount { get; }
}

public sealed class ItemBaseDefinition
{
    public ItemBaseDefinition(
        string id,
        string name,
        string iconId,
        string slot,
        int width,
        int height,
        int itemLevel,
        int dropWeight,
        Stats requirements,
        IntRange? damage,
        WeaponSwing? swing,
        IntRange? armor,
        int blockChance,
        PotionEffect? potion,
        bool ranged = false,
        string? ammoId = null,
        int stackMax = 1,
        string? spellId = null,
        int maxCharges = 0,
        int range = 0,
        int value = 0,
        int maxDurability = 0)
    {
        Id = id;
        Name = name;
        IconId = iconId;
        Slot = slot;
        Width = width;
        Height = height;
        ItemLevel = itemLevel;
        DropWeight = dropWeight;
        Requirements = requirements;
        Damage = damage;
        Swing = swing;
        Armor = armor;
        BlockChance = blockChance;
        Potion = potion;
        Ranged = ranged;
        AmmoId = ammoId;
        StackMax = stackMax;
        SpellId = spellId;
        MaxCharges = maxCharges;
        Range = range;
        Value = value;
        MaxDurability = maxDurability;
    }

    public string Id { get; }

    public string Name { get; }

    public string IconId { get; }

    public string Slot { get; }

    public int Width { get; }

    public int Height { get; }

    public int ItemLevel { get; }

    public int DropWeight { get; }

    public Stats Requirements { get; }

    public IntRange? Damage { get; }

    public WeaponSwing? Swing { get; }

    public IntRange? Armor { get; }

    public int BlockChance { get; }

    public PotionEffect? Potion { get; }

    public bool Ranged { get; }

    public string? AmmoId { get; }

    public int StackMax { get; }

    public string? SpellId { get; }

    public int MaxCharges { get; }

    public int Range { get; }

    public int Value { get; }

    public int MaxDurability { get; }

    public bool IsEquippable => ItemSlots.IsEquippable(Slot);

    public bool IsPotion => Slot == ItemSlots.Potion;

    public bool IsAmmo => Slot == ItemSlots.Ammo;

    public bool IsBook => Slot == ItemSlots.Book;

    public bool IsScroll => Slot == ItemSlots.Scroll;

    public bool IsStackable => StackMax > 1;
}
