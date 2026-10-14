using Mortar.Game;
using Mortar.Game.Content;
using Mortar.Game.Saves;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class SaveCaptureTests
{
    private static readonly AffixDefinition Keen =
        TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon);

    [Fact]
    public void Capture_RecordsTheWorldAndThePlayer()
    {
        var world = Create();
        TestWorlds.Run(world, 5);
        var snapshot = WorldSnapshot.Capture(world);
        Assert.Equal(WorldSnapshot.CurrentVersion, snapshot.Version);
        Assert.Equal(world.Seed, snapshot.Seed);
        Assert.Equal(5, snapshot.Tick);
        Assert.Equal(1, snapshot.Depth);
        Assert.Equal(world.NextItemId, snapshot.NextItemId);
        Assert.Equal(world.Rng.State, snapshot.RngState);
        var player = snapshot.Player!;
        Assert.Equal("warrior", player.ClassId);
        Assert.Equal(20, player.Stats!.Strength);
        Assert.Equal(Actor.PlayerId, player.Actor!.Id);
        Assert.Equal("Idle", player.Actor.Action);
        Assert.Equal(new[] { "Body", "Weapon" }, player.Equipment!.Select(e => e!.Slot));
        Assert.Equal(new[] { 0, 1 }, player.Belt!.Select(b => b!.Slot));
        var level = Assert.Single(snapshot.Levels!)!;
        Assert.Equal(world.Level.Layout.Fingerprint, level.Fingerprint);
    }

    [Fact]
    public void Capture_RecordsMonstersGroundItemsAndAffixes()
    {
        var world = Create();
        var zombie = TestWorlds.Spawn(world, "zombie", 5, 3);
        TestWorlds.DropGold(world, 4, 4, 9);
        var keenKnife = TestItems.Create(TestGameContent.Weapon(), world.NextItemId++, prefix: TestItems.Roll(Keen, 3));
        Assert.True(world.Belongings.Inventory.TryPlace(keenKnife, new Cell(4, 0)));
        var snapshot = WorldSnapshot.Capture(world);
        var level = snapshot.Levels![0]!;
        var monster = Assert.Single(level.Monsters!)!;
        Assert.Equal(zombie.Id, monster.Id);
        Assert.Equal("zombie", monster.Definition);
        Assert.Equal(9, Assert.Single(level.GroundItems!)!.Gold);
        var stored = Assert.Single(snapshot.Player!.Inventory!)!;
        Assert.Equal((4, 0), (stored.X, stored.Y));
        Assert.Equal("keen", stored.Item!.Prefix!.Id);
        Assert.Equal(3, stored.Item.Prefix.Value);
        Assert.Null(stored.Item.Suffix);
    }

    private static World Create() => TestWorlds.Create(TestGameContent.Valid(
        items: TestGameContent.Items(new[] { TestGameContent.Weapon(), TestGameContent.Armor(), TestGameContent.Potion() }, new[] { Keen }),
        classes: new[] { TestGameContent.Warrior("knife", "vest", "tonic", "tonic") }));
}
