using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class DurabilityTests
{
    private static World Arena(params ItemBaseDefinition[] bases) =>
        TestWorlds.Create(TestGameContent.Valid(
            rules: TestGameContent.Rules(minHit: 100, maxHit: 100),
            items: TestGameContent.Items(bases: bases)));

    [Fact]
    public void Factory_SpawnsItemsAtFullDurability()
    {
        var world = Arena(TestGameContent.Weapon(maxDurability: 40), TestGameContent.Potion());
        var knife = ItemFactory.CreatePlain(world, TestGameContent.Weapon(maxDurability: 40));
        Assert.Equal(40, knife.MaxDurability);
        Assert.Equal(40, knife.Durability);
        var tonic = ItemFactory.CreatePlain(world, TestGameContent.Potion());
        Assert.Equal(0, tonic.MaxDurability);
        Assert.Equal(0, tonic.Durability);
    }

    [Fact]
    public void PlayerMeleeHit_WearsWeaponByOne()
    {
        var knife = TestGameContent.Weapon(maxDurability: 10);
        var world = Arena(knife);
        var weapon = Equip(world, knife);
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        Assert.Equal(9, weapon.Durability);
        Assert.Equal(10, weapon.MaxDurability);
    }

    [Fact]
    public void PlayerMiss_DoesNotWearWeapon()
    {
        var knife = TestGameContent.Weapon(maxDurability: 10);
        var world = TestWorlds.Create(TestGameContent.Valid(
            rules: TestGameContent.Rules(minHit: 0, maxHit: 0),
            items: TestGameContent.Items(bases: new[] { knife })));
        var weapon = Equip(world, knife);
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        Assert.Equal(10, weapon.Durability);
    }

    [Fact]
    public void WeaponDurability_FloorsAtZero()
    {
        var knife = TestGameContent.Weapon(maxDurability: 1);
        var world = Arena(knife);
        var weapon = Equip(world, knife);
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        zombie.Life = 100;
        TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        Assert.Equal(0, weapon.Durability);
        world.Player.ClearAction();
        world.Player.SetAttackIntent(zombie.Id);
        TestWorlds.Run(world, 6);
        Assert.Equal(0, weapon.Durability);
    }

    [Fact]
    public void PlayerTakesHit_WearsBodyAndShield()
    {
        var vest = TestGameContent.Armor("vest", ItemSlots.Body, maxDurability: 20);
        var shield = TestGameContent.Armor(
            "shield", ItemSlots.Shield, armor: 2, width: 2, height: 2, blockChance: 0, maxDurability: 15);
        var world = Arena(vest, shield);
        var body = Equip(world, vest);
        var buckler = Equip(world, shield);
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        zombie.IsAwake = true;
        zombie.SetAttackIntent(Actor.PlayerId);
        var life = world.Player.Life;
        var hit = RunUntilPlayerHit(world);
        Assert.Equal(AttackOutcome.Hit, hit.Outcome);
        Assert.True(world.Player.Life < life);
        Assert.Equal(19, body.Durability);
        Assert.Equal(14, buckler.Durability);
    }

    [Fact]
    public void PlayerTakesHit_WithoutShield_WearsBodyOnly()
    {
        var vest = TestGameContent.Armor(maxDurability: 20);
        var world = Arena(vest);
        var body = Equip(world, vest);
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        zombie.IsAwake = true;
        zombie.SetAttackIntent(Actor.PlayerId);
        RunUntilPlayerHit(world);
        Assert.Equal(19, body.Durability);
    }

    [Fact]
    public void ArrowHit_WearsEquippedBow()
    {
        var arrows = new ItemBaseDefinition(
            "arrows", "Arrows", "arrows", ItemSlots.Ammo, 1, 1, 1, 1, default,
            null, null, null, 0, null, stackMax: 40, maxDurability: 0);
        var bow = new ItemBaseDefinition(
            "bow", "Bow", "bow", ItemSlots.Weapon, 1, 2, 1, 1, default,
            new IntRange(3, 3), new WeaponSwing(6, 3), null, 0, null,
            ranged: true, ammoId: "arrows", range: 8, value: 80, maxDurability: 30);
        var world = TestWorlds.Create(TestGameContent.Valid(
            rules: TestGameContent.Rules(minHit: 100, maxHit: 100),
            monsters: new[] { TestGameContent.Monster("zombie", sight: 1) },
            items: TestGameContent.Items(bases: new[] { bow, arrows }),
            classes: new[] { TestGameContent.Warrior("bow", "arrows") }));
        var weapon = world.Belongings.Equipment.Get(EquipmentSlot.Weapon)!;
        Assert.Equal(30, weapon.Durability);
        var zombie = TestWorlds.Spawn(world, "zombie", 5, 1);
        TestWorlds.Run(world, 12, new AttackCommand(zombie.Id));
        Assert.Equal(29, weapon.Durability);
    }

    [Fact]
    public void StaffSpellCast_DoesNotWearWeapon()
    {
        var staff = new ItemBaseDefinition(
            "staff", "Staff", "staff", ItemSlots.Weapon, 1, 3, 1, 0, default,
            new IntRange(1, 2), new WeaponSwing(8, 4), null, 0, null,
            spellId: "firebolt", maxCharges: 5, value: 100, maxDurability: 40);
        var spells = new SpellCatalog(new[]
        {
            new SpellDefinition(
                "firebolt", "Firebolt", SpellShapes.Projectile, 6, 6, 3, 8, new IntRange(4, 4), 0, 0, 2, 0),
        });
        var result = ContentSet.Create(
            TestGameContent.Rules(minHit: 100, maxHit: 100),
            new[] { TestGameContent.Warrior() },
            new[] { TestGameContent.Monster("zombie", sight: 1, life: 30) },
            new[] { TestGameContent.Level(1), TestGameContent.Level(2) },
            TestGameContent.Items(bases: new[] { staff }),
            spells);
        var world = TestWorlds.Create(result.Value!);
        var equipped = ItemFactory.CreatePlain(world, staff);
        world.Belongings.Equipment.Set(EquipmentSlot.Weapon, equipped);
        world.RefreshPlayer();
        var zombie = TestWorlds.Spawn(world, "zombie", 3, 1);
        TestWorlds.Run(world, 4, new CastCommand("firebolt", zombie.Id));
        Assert.Equal(40, equipped.Durability);
    }

    [Fact]
    public void BrokenGearContributesNothing()
    {
        var affix = TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.Damage, 3, ItemSlots.Weapon);
        var knife = TestGameContent.Weapon(maxDurability: 1, damageMin: 4, damageMax: 6);
        var world = TestWorlds.Create(TestGameContent.Valid(
            rules: TestGameContent.Rules(minHit: 100, maxHit: 100),
            items: TestGameContent.Items(bases: new[] { knife }, affixes: new[] { affix })));
        var weapon = new Item(world.NextItemId++, knife, 1, 0, TestItems.Roll(affix, 3), null);
        weapon.Durability = 0;
        world.Belongings.Equipment.Set(EquipmentSlot.Weapon, weapon);
        world.RefreshPlayer();
        var bonus = GearBonus.From(world.Belongings.Equipment);
        Assert.Equal(0, weapon.Durability);
        Assert.Equal(0, bonus.Damage);
        Assert.Null(bonus.WeaponDamage);
        Assert.Null(bonus.Swing);
    }

    [Fact]
    public void BreakingWeapon_RefreshesPlayerProfile()
    {
        var knife = TestGameContent.Weapon(maxDurability: 1, damageMin: 4, damageMax: 6);
        var world = Arena(knife);
        var weapon = Equip(world, knife);
        var armed = world.Player.Profile.Damage;
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        Assert.Equal(0, weapon.Durability);
        Assert.NotEqual(armed, world.Player.Profile.Damage);
        var expected = PlayerStats.Compute(
            world.PlayerClass, world.Progress, world.Content.Rules, GearBonus.From(world.Belongings.Equipment));
        Assert.Equal(expected.Damage, world.Player.Profile.Damage);
        Assert.Null(GearBonus.From(world.Belongings.Equipment).WeaponDamage);
    }

    private static Item Equip(World world, ItemBaseDefinition definition)
    {
        var item = TestWorlds.Give(world, definition);
        var rejected = world.Tick(new Command[] { new EquipCommand(item.Id) })
            .OfType<CommandRejected>()
            .FirstOrDefault();
        Assert.Null(rejected);
        return item;
    }

    private static AttackResolved RunUntilPlayerHit(World world)
    {
        for (var i = 0; i < 40; i++)
        {
            var hit = world.Tick(Array.Empty<Command>())
                .OfType<AttackResolved>()
                .FirstOrDefault(e => e.TargetId == Actor.PlayerId && e.Outcome == AttackOutcome.Hit);
            if (hit is not null) return hit;
        }
        throw new InvalidOperationException("player was never hit");
    }
}
