using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class PickupTests
{
    private static readonly ItemBaseDefinition Knife = TestGameContent.Weapon();
    private static readonly ItemBaseDefinition Vest = TestGameContent.Armor();
    private static readonly ItemBaseDefinition Tonic = TestGameContent.Potion();

    private static readonly string[] Walled =
    {
        "#########",
        "#E..#...#",
        "#...#...#",
        "#...#...#",
        "#...#..X#",
        "#########",
    };

    [Fact]
    public void AdjacentGold_IsPickedUpTheSameTick()
    {
        var world = Create();
        var gold = TestWorlds.DropGold(world, 2, 1, 7);
        var events = world.Tick(new Command[] { new PickUpCommand(gold.Id) });
        var picked = Assert.Single(events.OfType<GoldPickedUp>());
        Assert.Equal(7, picked.Amount);
        Assert.Equal(7, world.Belongings.Gold);
        Assert.Empty(world.Level.GroundItems);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void FarItem_IsWalkedToThenPickedUp()
    {
        var world = Create();
        var ground = TestWorlds.DropItem(world, 6, 3, Knife);
        var events = TestWorlds.Run(world, 80, new PickUpCommand(ground.Id));
        Assert.Single(events.OfType<ItemPickedUp>());
        Assert.NotNull(world.Belongings.Inventory.Find(ground.Id));
        Assert.True(world.Player.Cell.DistanceTo(new Cell(6, 3)) <= 1);
    }

    [Fact]
    public void Potion_GoesToTheBelt()
    {
        var world = Create();
        var ground = TestWorlds.DropItem(world, 2, 2, Tonic);
        world.Tick(new Command[] { new PickUpCommand(ground.Id) });
        Assert.Equal(ground.Id, world.Belongings.Belt.Get(0)!.Id);
    }

    [Fact]
    public void FullInventory_LeavesTheItemOnTheGround()
    {
        var world = Create();
        for (var id = 100; id < 110; id++) Assert.True(world.Belongings.Inventory.TryAdd(TestItems.Create(Vest, id)));
        var ground = TestWorlds.DropItem(world, 2, 1, Vest);
        var events = world.Tick(new Command[] { new PickUpCommand(ground.Id) });
        var failed = Assert.Single(events.OfType<PickupFailed>());
        Assert.Equal(PickupSystem.InventoryFull, failed.Reason);
        Assert.Same(ground, world.Level.FindGroundItem(ground.Id));
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void MissingItem_IsRejected()
    {
        var world = Create();
        var events = world.Tick(new Command[] { new PickUpCommand(99) });
        Assert.Equal("no such item on this level", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void WalledOffItem_IsRejected()
    {
        var world = Create(new[] { Walled, TestWorlds.Room });
        var ground = TestWorlds.DropItem(world, 6, 2, Knife);
        var events = world.Tick(new Command[] { new PickUpCommand(ground.Id) });
        Assert.Equal("item is unreachable", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void NewCommand_ReplacesThePickup()
    {
        var world = Create();
        var ground = TestWorlds.DropItem(world, 6, 3, Knife);
        world.Tick(new Command[] { new PickUpCommand(ground.Id) });
        TestWorlds.Run(world, 60, new MoveCommand(new Cell(1, 4)));
        Assert.Same(ground, world.Level.FindGroundItem(ground.Id));
        Assert.Equal(new Cell(1, 4), world.Player.Cell);
    }

    private static World Create(string[][]? levels = null) => TestWorlds.Create(
        TestGameContent.Valid(items: TestGameContent.Items(new[] { Knife, Vest, Tonic })), levels: levels);
}
