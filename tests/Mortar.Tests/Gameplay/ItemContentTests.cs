using Mortar.Core.Authoring;
using Mortar.Game.Content;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class ItemContentTests
{
    private static readonly AffixDefinition Keen =
        TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);

    [Fact]
    public void ValidItems_AreAvailableById()
    {
        var items = TestGameContent.Items(
            new[] { TestGameContent.Weapon(), TestGameContent.Potion() }, new[] { Keen }, new[] { TestGameContent.Loot() });
        var content = TestGameContent.Valid(items: items);
        Assert.Equal("knife", content.ItemBase("knife").Id);
        Assert.True(content.TryGetAffix("keen", out _));
        Assert.Equal(1, content.LootTable("loot").Rolls);
        Assert.Equal(2, content.ItemBases.Count);
    }

    [Fact]
    public void WeaponWithoutDamage_IsRejected()
    {
        var club = new ItemBaseDefinition("club", "club", "club", ItemSlots.Weapon, 1, 2, 1, 1, default, null, null, null, 0, null, value: 10, maxDurability: 20);
        AssertError(Create(bases: club), "item 'club'", "weapons need damage");
    }

    [Fact]
    public void NegativeValue_IsRejected()
    {
        var knife = TestGameContent.Weapon(value: -1);
        AssertError(Create(bases: knife), "item 'knife'", "value must be between 0");
    }

    [Fact]
    public void EquippableWithoutMaxDurability_IsRejected()
    {
        var knife = new ItemBaseDefinition(
            "knife", "knife", "knife", ItemSlots.Weapon, 1, 2, 1, 1, default,
            new IntRange(2, 4), null, null, 0, null, value: 25, maxDurability: 0);
        AssertError(Create(bases: knife), "item 'knife'", "equippable items need maxDurability of at least 1");
    }

    [Fact]
    public void PotionMayHaveZeroMaxDurability()
    {
        var tonic = TestGameContent.Potion(value: 40, maxDurability: 0);
        var content = TestGameContent.Valid(items: TestGameContent.Items(new[] { tonic }));
        Assert.Equal(40, content.ItemBase("tonic").Value);
        Assert.Equal(0, content.ItemBase("tonic").MaxDurability);
    }

    [Fact]
    public void ArmorOnARing_IsRejected()
    {
        var ring = new ItemBaseDefinition("ring", "ring", "ring", ItemSlots.Ring, 1, 1, 1, 1, default, null, null, new IntRange(1, 2), 0, null);
        AssertError(Create(bases: ring), "item 'ring'", "only body, head, and shield items have armor");
    }

    [Fact]
    public void LargePotion_IsRejected()
    {
        var jug = new ItemBaseDefinition("jug", "jug", "jug", ItemSlots.Potion, 1, 2, 1, 1, default, null, null, null, 0, new PotionEffect(PotionKinds.Life, 5));
        AssertError(Create(bases: jug), "item 'jug'", "rings, amulets, potions, ammo, books, and scrolls must be 1x1");
    }

    [Fact]
    public void UnknownSlot_IsRejected()
    {
        var sash = new ItemBaseDefinition("sash", "sash", "sash", "belt", 1, 1, 1, 1, default, null, null, null, 0, null);
        AssertError(Create(bases: sash), "item 'sash'", "unknown slot 'belt'");
    }

    [Fact]
    public void SwingWithHitFrameAtTheEnd_IsRejected()
    {
        AssertError(Create(bases: TestGameContent.Weapon(swing: new WeaponSwing(8, 8))), "item 'knife'", "swing needs 1 <= hitFrame < attackTicks");
    }

    [Fact]
    public void AffixForPotions_IsRejected()
    {
        var odd = TestGameContent.Affix("odd", AffixPositions.Suffix, BonusKinds.Life, 3, ItemSlots.Potion);
        AssertError(Create(affix: odd), "affix 'odd'", "slots must list one or more equippable slots");
    }

    [Fact]
    public void LootTableWithNoWeight_IsRejected()
    {
        AssertError(Create(loot: TestGameContent.Loot(item: 0)), "loot table 'loot'", "must add up to at least 1");
    }

    [Fact]
    public void GoldWeightWithoutAGoldRange_IsRejected()
    {
        var table = new LootTableDefinition("loot", 1, 0, 1, 0, new IntRange(0, 0), 0);
        AssertError(Create(loot: table), "loot table 'loot'", "gold must satisfy 1 <= min <= max");
    }

    [Fact]
    public void UnknownStartingItem_IsRejected()
    {
        var result = TestGameContent.Create(classes: new[] { TestGameContent.Warrior("sword") });
        AssertError(result, "class 'warrior'", "starting item references unknown item 'sword'");
    }

    [Fact]
    public void UnknownLootTable_IsRejected()
    {
        var result = TestGameContent.Create(monsters: new[] { TestGameContent.Monster("zombie", loot: "rare") });
        AssertError(result, "monster 'zombie'", "loot references unknown loot table 'rare'");
    }

    [Fact]
    public void DuplicateItemIds_AreRejected()
    {
        var items = TestGameContent.Items(new[] { TestGameContent.Weapon(), TestGameContent.Weapon() });
        AssertError(TestGameContent.Create(items: items), "item 'knife'", "duplicate id");
    }

    [Fact]
    public void Content_KeepsItsOwnCopiesOfLists()
    {
        var thresholds = new long[] { 100, 300, 600 };
        var spawns = new List<SpawnEntry> { new SpawnEntry("zombie", 1) };
        var bases = new List<ItemBaseDefinition> { TestGameContent.Weapon() };
        var slots = new List<string> { ItemSlots.Weapon };
        var kit = new List<string> { "knife" };
        var affix = new AffixDefinition("keen", "Keen", AffixPositions.Prefix, slots, new IntRange(1, 5), 1, BonusKinds.ToHit, new IntRange(1, 2));
        var warrior = TestGameContent.Warrior();
        var classes = new[]
        {
            new ClassDefinition(
                warrior.Id, warrior.Name, warrior.SpriteSet, warrior.BaseStats, warrior.StatCaps,
                warrior.Growth, warrior.UnarmedDamage, warrior.Timings, kit),
        };
        var content = TestGameContent.Valid(
            levels: new[] { new LevelDefinition(1, "test", spawns, null) },
            rules: TestGameContent.Rules(thresholds: thresholds),
            items: TestGameContent.Items(bases, new[] { affix }),
            classes: classes);
        thresholds[0] = 999;
        spawns.Clear();
        bases.Clear();
        slots.Clear();
        kit.Clear();
        Assert.Equal(100, content.Rules.ExperienceThresholds[0]);
        Assert.Single(content.Level(1).Spawns);
        Assert.Single(content.ItemBases);
        Assert.Single(content.Affixes[0].Slots);
        Assert.Single(content.Classes[0].StartingItems);
    }

    private static AuthoringResult<ContentSet> Create(
        ItemBaseDefinition? bases = null, AffixDefinition? affix = null, LootTableDefinition? loot = null) =>
        TestGameContent.Create(items: TestGameContent.Items(
            bases is null ? null : new[] { bases },
            affix is null ? null : new[] { affix },
            loot is null ? null : new[] { loot }));

    private static void AssertError(AuthoringResult<ContentSet> result, string source, string fragment)
    {
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Source == source && e.Message.Contains(fragment));
    }
}
