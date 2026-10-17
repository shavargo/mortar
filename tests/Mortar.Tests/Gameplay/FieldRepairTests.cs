using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class FieldRepairTests
{
    [Fact]
    public void WarriorRepair_RestoresDurabilityAndReducesMax()
    {
        var knife = TestGameContent.Weapon(maxDurability: 30);
        var world = Arena(knife);
        var item = Equip(world, knife);
        item.Durability = 10;

        var events = world.Tick(new Command[] { new ClassSkillCommand(item.Id) });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.True(item.Durability > 10);
        Assert.True(item.MaxDurability < 30);
        Assert.True(item.Durability <= item.MaxDurability);
        var repaired = Assert.Single(events.OfType<ItemFieldRepaired>());
        Assert.Equal(item.Id, repaired.ItemId);
        Assert.Equal(item.Durability, repaired.Durability);
        Assert.Equal(item.MaxDurability, repaired.MaxDurability);
    }

    [Fact]
    public void WarriorRepair_MissingItemId_Rejects()
    {
        var world = Arena(TestGameContent.Weapon());
        var events = world.Tick(new Command[] { new ClassSkillCommand() });
        Assert.Equal("repair needs an item", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void WarriorRepair_MissingItem_Rejects()
    {
        var world = Arena(TestGameContent.Weapon());
        var events = world.Tick(new Command[] { new ClassSkillCommand(999) });
        Assert.Equal("no such item", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void WarriorRepair_NotEquipped_Rejects()
    {
        var knife = TestGameContent.Weapon(maxDurability: 30);
        var world = Arena(knife);
        var item = TestWorlds.Give(world, knife);
        item.Durability = 10;

        var events = world.Tick(new Command[] { new ClassSkillCommand(item.Id) });

        Assert.Equal("item is not equipped", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Equal(10, item.Durability);
        Assert.Equal(30, item.MaxDurability);
    }

    [Fact]
    public void WarriorRepair_AlreadyFull_Rejects()
    {
        var knife = TestGameContent.Weapon(maxDurability: 30);
        var world = Arena(knife);
        var item = Equip(world, knife);

        var events = world.Tick(new Command[] { new ClassSkillCommand(item.Id) });

        Assert.Equal("item does not need repair", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void WarriorRepair_NonDurable_Rejects()
    {
        var knife = TestGameContent.Weapon(maxDurability: 30);
        var world = Arena(knife);
        var item = Equip(world, knife);
        item.MaxDurability = 0;
        item.Durability = 0;

        var events = world.Tick(new Command[] { new ClassSkillCommand(item.Id) });

        Assert.Equal("item cannot be repaired", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void WarriorRepair_CanDestroyFragileItem()
    {
        var knife = TestGameContent.Weapon(maxDurability: 1);
        var world = Arena(knife);
        var item = Equip(world, knife);
        item.Durability = 0;
        var id = item.Id;

        var events = world.Tick(new Command[] { new ClassSkillCommand(id) });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Null(world.Belongings.Find(id));
        Assert.Null(world.Belongings.Equipment.Get(EquipmentSlot.Weapon));
        var destroyed = Assert.Single(events.OfType<ItemDestroyed>());
        Assert.Equal(id, destroyed.ItemId);
    }

    [Fact]
    public void NoClassSkill_Rejects()
    {
        var classes = new[]
        {
            new ClassDefinition(
                "novice", "Novice", "novice",
                new Stats(10, 10, 10, 10),
                new Stats(100, 100, 100, 100),
                new ClassGrowth(10, 1, 1, 0, 1, 1, 5),
                new IntRange(1, 2),
                TestGameContent.Timings(),
                Array.Empty<string>()),
        };
        var world = TestWorlds.Create(TestGameContent.Valid(classes: classes), classId: "novice");
        var events = world.Tick(new Command[] { new ClassSkillCommand(1) });
        Assert.Equal("no class skill", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void SorcererRecharge_IgnoresItemId()
    {
        var loaded = Mortar.Json.Content.GameContentLoader.Load(Mortar.Tests.Support.ContentFixture.Manifest);
        var world = World.Create(loaded.Value!.Content, loaded.Value.CreateLevelGenerator(), "sorcerer", 1).World!;
        var staff = world.Belongings.Equipment.Get(EquipmentSlot.Weapon)!;
        staff.Charges = 0;
        var mana = world.Player.Mana;

        var events = world.Tick(new Command[] { new ClassSkillCommand(999) });

        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Equal(staff.Definition.MaxCharges, staff.Charges);
        Assert.Equal(mana - staff.Definition.MaxCharges, world.Player.Mana);
        Assert.Contains(events.OfType<StaffRecharged>(), e => e.Charges == staff.Charges);
    }

    private static World Arena(params ItemBaseDefinition[] bases) =>
        TestWorlds.Create(TestGameContent.Valid(
            items: TestGameContent.Items(bases: bases),
            classes: new[] { WarriorWithRepair() }));

    private static ClassDefinition WarriorWithRepair() => new ClassDefinition(
        "warrior",
        "Warrior",
        "warrior",
        new Stats(20, 20, 20, 10),
        new Stats(100, 100, 100, 100),
        new ClassGrowth(10, 2, 2, 0, 1, 1, 5),
        new IntRange(2, 4),
        TestGameContent.Timings(),
        Array.Empty<string>(),
        classSkill: ClassSkills.Repair);

    private static Item Equip(World world, ItemBaseDefinition definition)
    {
        var item = TestWorlds.Give(world, definition);
        var rejected = world.Tick(new Command[] { new EquipCommand(item.Id) })
            .OfType<CommandRejected>()
            .FirstOrDefault();
        Assert.Null(rejected);
        return item;
    }
}
