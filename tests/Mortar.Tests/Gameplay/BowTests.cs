using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class BowTests
{
    private static readonly ItemBaseDefinition Arrows = new ItemBaseDefinition(
        "arrows", "Arrows", "arrows", ItemSlots.Ammo, 1, 1, 1, 1, default,
        null, null, null, 0, null, stackMax: 40);

    private static readonly ItemBaseDefinition Bow = new ItemBaseDefinition(
        "bow", "Bow", "bow", ItemSlots.Weapon, 1, 2, 1, 1, default,
        new IntRange(3, 3), new WeaponSwing(6, 3), null, 0, null,
        ranged: true, ammoId: "arrows", range: 8, value: 80, maxDurability: 30);

    private static ContentSet BowContent() =>
        TestGameContent.Valid(
            rules: TestGameContent.Rules(minHit: 100, maxHit: 100),
            items: TestGameContent.Items(bases: new[] { Bow, Arrows, TestGameContent.Potion() }),
            classes: new[]
            {
                TestGameContent.Warrior("bow", "arrows"),
            });

    private static World Arena()
    {
        var world = TestWorlds.Create(BowContent());
        world.RefreshPlayer();
        return world;
    }

    [Fact]
    public void RangedAttack_ConsumesAmmoAndSpawnsArrow()
    {
        var world = Arena();
        var ammo = world.Belongings.Belt.Get(0) ?? world.Belongings.Inventory.Items[0].Item;
        Assert.Equal("arrows", ammo.Definition.Id);
        var before = ammo.StackCount;
        var zombie = TestWorlds.Spawn(world, "zombie", 4, 1);
        zombie.IsAwake = false;
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        Assert.Equal(before - 1, ammo.StackCount);
        Assert.Contains(events.OfType<ProjectileSpawned>(), e => e.Kind == ProjectileKinds.Arrow);
        Assert.Contains(events.OfType<AttackStarted>(), e => e.AttackerId == Actor.PlayerId);
    }

    [Fact]
    public void RangedAttack_WithoutAmmo_IsRejected()
    {
        var world = Arena();
        while (Ammo.Has(world.Belongings, "arrows"))
        {
            Ammo.TrySpend(world.Belongings, "arrows");
        }
        var zombie = TestWorlds.Spawn(world, "zombie", 3, 1);
        var events = world.Tick(new Command[] { new AttackCommand(zombie.Id) });
        var rejected = Assert.Single(events.OfType<CommandRejected>());
        Assert.Equal("no ammo", rejected.Reason);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void Arrow_HitsAtRange()
    {
        var world = TestWorlds.Create(TestGameContent.Valid(
            rules: TestGameContent.Rules(minHit: 100, maxHit: 100),
            monsters: new[] { TestGameContent.Monster("zombie", sight: 1) },
            items: TestGameContent.Items(bases: new[] { Bow, Arrows }),
            classes: new[] { TestGameContent.Warrior("bow", "arrows") }));
        var zombie = TestWorlds.Spawn(world, "zombie", 5, 1);
        var life = zombie.Life;
        var events = TestWorlds.Run(world, 12, new AttackCommand(zombie.Id));
        Assert.Contains(events.OfType<ProjectileHit>(), e => e.TargetId == zombie.Id);
        Assert.True(zombie.Life < life);
    }

    [Fact]
    public void MeleeWeapon_StillMelees()
    {
        var world = TestWorlds.Create(TestGameContent.Valid(rules: TestGameContent.Rules(minHit: 100, maxHit: 100)));
        var zombie = TestWorlds.Spawn(world, "zombie", 2, 1);
        var events = TestWorlds.Run(world, 4, new AttackCommand(zombie.Id));
        Assert.Contains(events.OfType<AttackResolved>(), e => e.AttackerId == Actor.PlayerId);
        Assert.Empty(events.OfType<ProjectileSpawned>());
    }
}
