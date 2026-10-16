using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class IdentifyTests
{
    [Fact]
    public void MagicLoot_IsUnidentifiedAndAffixesDoNotApply()
    {
        var weapon = TestGameContent.Weapon();
        var affix = TestGameContent.Affix("sharp", AffixPositions.Prefix, BonusKinds.Damage, 5, ItemSlots.Weapon);
        var world = TestWorlds.Create(TestGameContent.Valid(
            items: TestGameContent.Items(bases: new[] { weapon }, affixes: new[] { affix })));
        var item = new Item(world.NextItemId++, weapon, 1, 0, TestItems.Roll(affix, 5), null, identified: false);
        Assert.Equal("Magic weapon", item.Name);
        world.Belongings.Equipment.Set(EquipmentSlot.Weapon, item);
        world.RefreshPlayer();
        var withHidden = world.Player.Profile.Damage;
        item.Identified = true;
        world.RefreshPlayer();
        var withShown = world.Player.Profile.Damage;
        Assert.True(withShown.Min > withHidden.Min || withShown.Max > withHidden.Max);
        Assert.Contains("sharp", item.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IdentifySpell_RevealsItem()
    {
        var weapon = TestGameContent.Weapon();
        var affix = TestGameContent.Affix("sharp", AffixPositions.Prefix, BonusKinds.Damage, 5, ItemSlots.Weapon);
        var identify = new SpellDefinition(
            "identify", "Identify", SpellShapes.Self, 8, 6, 3, 0, null, 0, 0, 0, 0);
        var result = ContentSet.Create(
            TestGameContent.Rules(),
            new[]
            {
                new ClassDefinition(
                    "seer", "Seer", "seer",
                    new Stats(10, 10, 10, 30),
                    new Stats(100, 100, 100, 100),
                    new ClassGrowth(10, 1, 1, 20, 2, 2, 5),
                    new IntRange(1, 2),
                    TestGameContent.Timings(),
                    Array.Empty<string>(),
                    new[] { "identify" }),
            },
            new[] { TestGameContent.Monster("zombie") },
            new[] { TestGameContent.Level(1), TestGameContent.Level(2) },
            TestGameContent.Items(bases: new[] { weapon }, affixes: new[] { affix }),
            new SpellCatalog(new[] { identify }));
        var world = TestWorlds.Create(result.Value!, classId: "seer");
        var item = new Item(world.NextItemId++, weapon, 1, 0, TestItems.Roll(affix, 5), null, identified: false);
        Assert.True(world.Belongings.Stow(item));
        var events = TestWorlds.Run(world, 4, new CastCommand("identify", itemId: item.Id));
        Assert.True(item.Identified);
        Assert.Contains(events.OfType<ItemIdentified>(), e => e.ItemId == item.Id);
    }
}
