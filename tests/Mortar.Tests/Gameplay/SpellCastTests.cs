using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Tests.Gameplay.Support;
using Mortar.Tests.Support;

namespace Mortar.Tests.Gameplay;

public class SpellCastTests
{
    private static SpellCatalog Spells() => new SpellCatalog(new[]
    {
        new SpellDefinition("firebolt", "Firebolt", SpellShapes.Projectile, 6, 6, 3, 8, new IntRange(4, 4), 0, 0, 2, 0),
        new SpellDefinition("healing", "Healing", SpellShapes.Self, 10, 6, 3, 0, null, 20, 0, 0, 5),
        new SpellDefinition("lightning", "Lightning", SpellShapes.Projectile, 12, 6, 3, 10, new IntRange(6, 6), 0, 0, 3, 0),
    });

    private static World MageWorld()
    {
        var result = ContentSet.Create(
            TestGameContent.Rules(minHit: 100, maxHit: 100),
            new[]
            {
                new ClassDefinition(
                    "mage", "Mage", "mage",
                    new Stats(10, 10, 10, 40),
                    new Stats(100, 100, 100, 100),
                    new ClassGrowth(10, 1, 1, 30, 2, 2, 5),
                    new IntRange(1, 2),
                    TestGameContent.Timings(),
                    Array.Empty<string>(),
                    new[] { "firebolt", "healing", "lightning" }),
            },
            new[] { TestGameContent.Monster("zombie", sight: 1) },
            new[] { TestGameContent.Level(1), TestGameContent.Level(2) },
            TestGameContent.Items(),
            Spells());
        var content = result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors));
        return TestWorlds.Create(content, classId: "mage");
    }

    [Fact]
    public void CastFirebolt_SpendsManaAndSpawnsProjectile()
    {
        var world = MageWorld();
        var mana = world.Player.Mana;
        var zombie = TestWorlds.Spawn(world, "zombie", 4, 1);
        var events = TestWorlds.Run(world, 4, new CastCommand("firebolt", zombie.Id));
        Assert.Equal(mana - 6, world.Player.Mana);
        Assert.Contains(events.OfType<SpellCastStarted>(), e => e.SpellId == "firebolt");
        Assert.Contains(events.OfType<ProjectileSpawned>(), e => e.Kind == "firebolt");
    }

    [Fact]
    public void CastHealing_RestoresLife()
    {
        var world = MageWorld();
        world.Player.Life = 5;
        var mana = world.Player.Mana;
        var events = TestWorlds.Run(world, 4, new CastCommand("healing"));
        Assert.Equal(mana - 10, world.Player.Mana);
        var healed = Assert.Single(events.OfType<SpellHealed>());
        Assert.Equal(world.Player.Profile.MaxLife, world.Player.Life);
        Assert.Equal(world.Player.Profile.MaxLife - 5, healed.Amount);
    }

    [Fact]
    public void Cast_UnknownOrUnlearned_IsRejected()
    {
        var world = MageWorld();
        var unknown = world.Tick(new Command[] { new CastCommand("nope") });
        Assert.Equal("unknown spell", Assert.Single(unknown.OfType<CommandRejected>()).Reason);

        var blank = ContentSet.Create(
            TestGameContent.Rules(),
            new[] { TestGameContent.Warrior() },
            new[] { TestGameContent.Monster("zombie") },
            new[] { TestGameContent.Level(1), TestGameContent.Level(2) },
            TestGameContent.Items(),
            Spells());
        var warrior = TestWorlds.Create(blank.Value!);
        var unlearned = warrior.Tick(new Command[] { new CastCommand("firebolt", targetCell: new Cell(3, 1)) });
        Assert.Equal("spell not learned", Assert.Single(unlearned.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void Cast_WithoutMana_IsRejected()
    {
        var world = MageWorld();
        world.Player.Mana = 0;
        var zombie = TestWorlds.Spawn(world, "zombie", 3, 1);
        var events = world.Tick(new Command[] { new CastCommand("firebolt", zombie.Id) });
        Assert.Equal("not enough mana", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void SorcererKit_KnowsStarterSpells()
    {
        var loaded = GameContentLoader.Load(ContentFixture.Manifest);
        Assert.True(loaded.Succeeded, string.Join("; ", loaded.Errors));
        var world = World.Create(loaded.Value!.Content, loaded.Value.CreateLevelGenerator(), "sorcerer", 1).World!;
        Assert.True(world.Progress.Knows("firebolt"));
        Assert.True(world.Progress.Knows("healing"));
        Assert.True(world.Progress.Knows("lightning"));
        Assert.Equal(1, world.Progress.SpellLevel("firebolt"));
        Assert.True(world.Player.Mana > 0);
    }
}
