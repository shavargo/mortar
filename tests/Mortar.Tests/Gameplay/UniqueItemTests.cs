using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class UniqueItemTests
{
    [Fact]
    public void Quality_IsUniqueWhenUniqueIdSet()
    {
        var knife = TestGameContent.Weapon();
        var item = TestItems.CreateUnique(knife, "relic", "Relic Blade", Array.Empty<RolledAffix>());
        Assert.Equal(ItemQuality.Unique, item.Quality);
        Assert.Equal("relic", item.UniqueId);
    }

    [Fact]
    public void Quality_PrefersUniqueOverMagicAffixes()
    {
        var affix = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.Damage, 3, ItemSlots.Weapon);
        var item = new Item(
            1,
            TestGameContent.Weapon(),
            1,
            0,
            TestItems.Roll(affix, 3),
            null,
            uniqueId: "relic",
            uniqueName: "Relic Blade",
            uniquePowers: Array.Empty<RolledAffix>());
        Assert.Equal(ItemQuality.Unique, item.Quality);
    }

    [Fact]
    public void Name_UnidentifiedUnique_UsesBaseName()
    {
        var knife = TestGameContent.Weapon();
        var item = TestItems.CreateUnique(
            knife, "relic", "Relic Blade", Array.Empty<RolledAffix>(), identified: false);
        Assert.Equal(knife.Name, item.Name);
    }

    [Fact]
    public void Name_IdentifiedUnique_UsesUniqueName()
    {
        var item = TestItems.CreateUnique(
            TestGameContent.Weapon(), "relic", "Relic Blade", Array.Empty<RolledAffix>());
        Assert.Equal("Relic Blade", item.Name);
    }

    [Fact]
    public void GearBonus_AppliesUniquePowersWhenIdentified()
    {
        var damage = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.Damage, 3, ItemSlots.Weapon);
        var powers = new[] { TestItems.Roll(damage, 3) };
        var equipment = new Equipment();
        equipment.Set(
            EquipmentSlot.Weapon,
            TestItems.CreateUnique(TestGameContent.Weapon(), "relic", "Relic Blade", powers));
        Assert.Equal(3, GearBonus.From(equipment).Damage);
    }

    [Fact]
    public void GearBonus_IgnoresUniquePowersWhenUnidentified()
    {
        var damage = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.Damage, 3, ItemSlots.Weapon);
        var powers = new[] { TestItems.Roll(damage, 3) };
        var equipment = new Equipment();
        equipment.Set(
            EquipmentSlot.Weapon,
            TestItems.CreateUnique(
                TestGameContent.Weapon(), "relic", "Relic Blade", powers, identified: false));
        Assert.Equal(0, GearBonus.From(equipment).Damage);
    }

    [Fact]
    public void Pricing_UsesUniqueValueWhenPositive()
    {
        var rules = TestGameContent.Rules(vendorMagicBuyMult: 3, vendorSellPercent: 25);
        var item = TestItems.CreateUnique(
            TestGameContent.Weapon(value: 50),
            "relic",
            "Relic Blade",
            Array.Empty<RolledAffix>(),
            uniqueValue: 200);
        Assert.Equal(200, Pricing.BuyPrice(item, rules));
        Assert.Equal(50, Pricing.SellPrice(item, rules));
    }

    [Fact]
    public void Pricing_UsesMagicBuyMultWhenUniqueValueIsZero()
    {
        var rules = TestGameContent.Rules(vendorMagicBuyMult: 3, vendorSellPercent: 25);
        var item = TestItems.CreateUnique(
            TestGameContent.Weapon(value: 50),
            "relic",
            "Relic Blade",
            Array.Empty<RolledAffix>());
        Assert.Equal(150, Pricing.BuyPrice(item, rules));
        Assert.Equal(37, Pricing.SellPrice(item, rules));
    }

    [Fact]
    public void MaxDurability_IsMutable()
    {
        var item = TestItems.Create(TestGameContent.Weapon(maxDurability: 40));
        Assert.Equal(40, item.MaxDurability);
        item.MaxDurability = 25;
        Assert.Equal(25, item.MaxDurability);
    }

    [Fact]
    public void GearBonus_BrokenUniqueContributesNothing()
    {
        var damage = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.Damage, 3, ItemSlots.Weapon);
        var item = TestItems.CreateUnique(
            TestGameContent.Weapon(damageMin: 4, damageMax: 6),
            "relic",
            "Relic Blade",
            new[] { TestItems.Roll(damage, 3) });
        item.Durability = 0;
        var equipment = new Equipment();
        equipment.Set(EquipmentSlot.Weapon, item);
        var bonus = GearBonus.From(equipment);
        Assert.Equal(0, bonus.Damage);
        Assert.Null(bonus.WeaponDamage);
    }
}
