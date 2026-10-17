using Mortar.Game;
using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class GearTests
{
    private static CombatProfile Compute(Equipment equipment)
    {
        var warrior = TestGameContent.Warrior();
        return PlayerStats.Compute(warrior, PlayerProgress.Start(warrior), TestGameContent.Rules(), GearBonus.From(equipment));
    }

    [Fact]
    public void Weapon_ReplacesUnarmedDamageAndSwing()
    {
        var equipment = new Equipment();
        var knife = TestGameContent.Weapon(damageMin: 3, damageMax: 5, swing: new WeaponSwing(10, 5));
        equipment.Set(EquipmentSlot.Weapon, TestItems.Create(knife));
        var profile = Compute(equipment);
        Assert.Equal(5, profile.Damage.Min);
        Assert.Equal(7, profile.Damage.Max);
        Assert.Equal(10, profile.Timings.AttackTicks);
        Assert.Equal(5, profile.Timings.HitFrame);
        Assert.Equal(TestGameContent.Timings().MoveTicks, profile.Timings.MoveTicks);
    }

    [Fact]
    public void ArmorShieldAndAffixes_AddUp()
    {
        var equipment = new Equipment();
        var hale = TestGameContent.Affix("hale", AffixPositions.Prefix, BonusKinds.Life, 7, ItemSlots.Ring);
        var brawn = TestGameContent.Affix("brawn", AffixPositions.Suffix, BonusKinds.Strength, 10, ItemSlots.Ring);
        equipment.Set(EquipmentSlot.Body, TestItems.Create(TestGameContent.Armor(), 1, armor: 5));
        equipment.Set(EquipmentSlot.Shield, TestItems.Create(TestGameContent.Armor("shield", ItemSlots.Shield, blockChance: 20), 2, armor: 3));
        equipment.Set(EquipmentSlot.LeftRing, TestItems.Create(TestGameContent.Ring(), 3, prefix: TestItems.Roll(hale, 7), suffix: TestItems.Roll(brawn, 10)));
        var profile = Compute(equipment);
        Assert.Equal(12, profile.ArmorClass);
        Assert.Equal(20, profile.BlockChance);
        Assert.Equal(57, profile.MaxLife);
        Assert.Equal(5, profile.Damage.Min);
        Assert.Equal(7, profile.Damage.Max);
    }

    [Fact]
    public void World_StartsWithTheClassKit()
    {
        var world = TestWorlds.Create(KitContent("knife", "vest", "tonic", "tonic"));
        var belongings = world.Belongings;
        Assert.Equal(1, belongings.Equipment.Get(EquipmentSlot.Weapon)!.Id);
        Assert.Equal(2, belongings.Equipment.Get(EquipmentSlot.Body)!.Id);
        Assert.Equal(3, belongings.Belt.Get(0)!.Id);
        Assert.Equal(4, belongings.Belt.Get(1)!.Id);
        Assert.Equal(9, world.Player.Profile.ArmorClass);
        Assert.Equal(5, world.Player.Profile.Damage.Min);
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
        Assert.Equal(
            5 + world.Smith.Stock.Count + world.Healer.Stock.Count + world.Sage.Stock.Count,
            world.NextItemId);
    }

    [Fact]
    public void World_StowsKitItemsWhoseSlotIsTaken()
    {
        var world = TestWorlds.Create(KitContent("knife", "knife"));
        Assert.Equal(1, world.Belongings.Equipment.Get(EquipmentSlot.Weapon)!.Id);
        Assert.Equal(new Cell(0, 0), world.Belongings.Inventory.Find(2)!.Cell);
    }

    private static ContentSet KitContent(params string[] kit) => TestGameContent.Valid(
        items: TestGameContent.Items(new[] { TestGameContent.Weapon(), TestGameContent.Armor(), TestGameContent.Potion() }),
        classes: new[] { TestGameContent.Warrior(kit) });
}
