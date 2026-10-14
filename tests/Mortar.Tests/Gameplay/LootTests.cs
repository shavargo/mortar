using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class LootTests
{
    private static readonly AffixDefinition[] WeaponAffixes =
    {
        TestGameContent.Affix("Keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon),
        TestGameContent.Affix("of Brawn", AffixPositions.Suffix, BonusKinds.Strength, 2, ItemSlots.Weapon),
    };

    [Fact]
    public void KilledMonster_DropsAnItemOnItsCell()
    {
        var world = CreateWorld(TestGameContent.Loot(), TestGameContent.Weapon());
        var events = KillZombie(world, out var zombieCell);
        var dropped = Assert.Single(events.OfType<ItemDropped>());
        Assert.Equal("knife", dropped.Description);
        var ground = Assert.Single(world.Level.GroundItems);
        Assert.Equal(zombieCell, ground.Cell);
        Assert.Equal(ground.Item!.Id, ground.Id);
    }

    [Fact]
    public void GoldRoll_DropsAPileOfTheTableAmount()
    {
        var world = CreateWorld(TestGameContent.Loot(gold: 1, item: 0), TestGameContent.Weapon());
        KillZombie(world, out _);
        var ground = Assert.Single(world.Level.GroundItems);
        Assert.True(ground.IsGold);
        Assert.Equal(5, ground.Gold);
        Assert.Equal("5 gold", ground.Describe());
    }

    [Fact]
    public void NoneRoll_DropsNothing()
    {
        var world = CreateWorld(TestGameContent.Loot(none: 1, item: 0), TestGameContent.Weapon());
        KillZombie(world, out _);
        Assert.Empty(world.Level.GroundItems);
    }

    [Fact]
    public void SeveralDrops_SpreadOverFreeFloor()
    {
        var world = CreateWorld(TestGameContent.Loot(rolls: 4), TestGameContent.Weapon());
        KillZombie(world, out var zombieCell);
        var cells = world.Level.GroundItems.Select(g => g.Cell).ToList();
        Assert.Equal(4, cells.Distinct().Count());
        Assert.Equal(zombieCell, cells[0]);
        Assert.All(cells, cell => Assert.True(world.Level.IsWalkable(cell)));
        Assert.All(cells, cell => Assert.True(cell.DistanceTo(zombieCell) <= LevelState.DropRadius));
    }

    [Fact]
    public void MagicChance_RollsAffixes()
    {
        var world = CreateWorld(TestGameContent.Loot(magicPercent: 100), TestGameContent.Weapon());
        KillZombie(world, out _);
        var item = world.Level.GroundItems.Single().Item!;
        Assert.Equal(ItemQuality.Magic, item.Quality);
        Assert.NotEqual("knife", item.Name);
    }

    [Fact]
    public void NoEligibleAffix_LeavesTheItemNormal()
    {
        var ringOnly = new[] { TestGameContent.Affix("Hale", AffixPositions.Prefix, BonusKinds.Life, 3, ItemSlots.Ring) };
        var world = CreateWorld(TestGameContent.Loot(magicPercent: 100), new[] { TestGameContent.Weapon() }, ringOnly);
        KillZombie(world, out _);
        Assert.Equal(ItemQuality.Normal, world.Level.GroundItems.Single().Item!.Quality);
    }

    [Fact]
    public void PotionsAreNeverMagic()
    {
        var world = CreateWorld(TestGameContent.Loot(magicPercent: 100), TestGameContent.Potion());
        KillZombie(world, out _);
        Assert.Equal(ItemQuality.Normal, world.Level.GroundItems.Single().Item!.Quality);
    }

    [Fact]
    public void BasesAboveTheMonsterLevel_NeverDrop()
    {
        var world = CreateWorld(TestGameContent.Loot(rolls: 10), TestGameContent.Weapon(), TestGameContent.Weapon("sword", itemLevel: 5));
        KillZombie(world, out _);
        Assert.Equal(10, world.Level.GroundItems.Count);
        Assert.All(world.Level.GroundItems, ground => Assert.Equal("knife", ground.Item!.Definition.Id));
    }

    [Fact]
    public void SameSeed_GivesTheSameDrops()
    {
        static string Drops()
        {
            var world = CreateWorld(TestGameContent.Loot(rolls: 3, gold: 1, magicPercent: 50), TestGameContent.Weapon(), TestGameContent.Armor());
            KillZombie(world, out _);
            return string.Join("|", world.Level.GroundItems.Select(g => $"{g.Cell}:{g.Describe()}"));
        }
        Assert.Equal(Drops(), Drops());
    }

    private static World CreateWorld(LootTableDefinition table, params ItemBaseDefinition[] bases) =>
        CreateWorld(table, bases, WeaponAffixes);

    private static World CreateWorld(LootTableDefinition table, ItemBaseDefinition[] bases, AffixDefinition[] affixes)
    {
        var content = TestGameContent.Valid(
            monsters: new[] { TestGameContent.Monster("zombie", loot: table.Id) },
            rules: TestGameContent.Rules(minHit: 95),
            items: TestGameContent.Items(bases, affixes, new[] { table }));
        return TestWorlds.Create(content, seed: 3);
    }

    private static List<GameEvent> KillZombie(World world, out Cell zombieCell)
    {
        var zombie = TestWorlds.Spawn(world, "zombie", 3, 2);
        zombie.Life = 1;
        zombieCell = zombie.Cell;
        world.Player.Life = 1000;
        var events = TestWorlds.Run(world, 120, new AttackCommand(zombie.Id));
        Assert.True(zombie.IsDead);
        return events;
    }
}
