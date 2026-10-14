using Mortar.Game.Content;

namespace Mortar.Game.Items;

public sealed class GearBonus
{
    private GearBonus(Totals totals, IntRange? weaponDamage, WeaponSwing? swing)
    {
        Stats = new Stats(totals.Strength, totals.Dexterity, totals.Vitality, totals.Magic);
        Life = totals.Life;
        Mana = totals.Mana;
        ArmorClass = totals.ArmorClass;
        ToHit = totals.ToHit;
        Damage = totals.Damage;
        BlockChance = totals.BlockChance;
        WeaponDamage = weaponDamage;
        Swing = swing;
    }

    public static GearBonus None { get; } = new GearBonus(new Totals(), null, null);

    public Stats Stats { get; }

    public int Life { get; }

    public int Mana { get; }

    public int ArmorClass { get; }

    public int ToHit { get; }

    public int Damage { get; }

    public int BlockChance { get; }

    public IntRange? WeaponDamage { get; }

    public WeaponSwing? Swing { get; }

    public static GearBonus From(Equipment equipment)
    {
        var totals = new Totals();
        IntRange? weaponDamage = null;
        WeaponSwing? swing = null;
        foreach (var item in equipment.Worn)
        {
            totals.ArmorClass += item.ArmorValue;
            totals.BlockChance += item.Definition.BlockChance;
            if (item.Definition.Slot == ItemSlots.Weapon)
            {
                weaponDamage = item.Definition.Damage;
                swing = item.Definition.Swing;
            }
            if (item.Prefix is not null) totals.Apply(item.Prefix);
            if (item.Suffix is not null) totals.Apply(item.Suffix);
        }
        return new GearBonus(totals, weaponDamage, swing);
    }

    private sealed class Totals
    {
        public int Strength { get; set; }

        public int Dexterity { get; set; }

        public int Vitality { get; set; }

        public int Magic { get; set; }

        public int Life { get; set; }

        public int Mana { get; set; }

        public int ArmorClass { get; set; }

        public int ToHit { get; set; }

        public int Damage { get; set; }

        public int BlockChance { get; set; }

        public void Apply(RolledAffix affix)
        {
            var value = affix.Value;
            switch (affix.Definition.Bonus)
            {
                case BonusKinds.Strength: Strength += value; break;
                case BonusKinds.Dexterity: Dexterity += value; break;
                case BonusKinds.Vitality: Vitality += value; break;
                case BonusKinds.Magic: Magic += value; break;
                case BonusKinds.Life: Life += value; break;
                case BonusKinds.Mana: Mana += value; break;
                case BonusKinds.ArmorClass: ArmorClass += value; break;
                case BonusKinds.ToHit: ToHit += value; break;
                case BonusKinds.Damage: Damage += value; break;
            }
        }
    }
}
