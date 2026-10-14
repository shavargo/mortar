using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Saves;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class SaveRestoreTests
{
    private static readonly ContentSet Content = TestGameContent.Valid(
        monsters: new[] { TestGameContent.Monster("zombie", loot: "loot") },
        rules: TestGameContent.Rules(minHit: 95),
        items: TestGameContent.Items(
            new[] { TestGameContent.Weapon(), TestGameContent.Armor(), TestGameContent.Potion() },
            new[] { TestGameContent.Affix("keen", AffixPositions.Prefix, BonusKinds.ToHit, 3, ItemSlots.Weapon) },
            new[] { TestGameContent.Loot(gold: 1, item: 1, magicPercent: 50) }),
        classes: new[] { TestGameContent.Warrior("knife", "vest", "tonic", "tonic") });

    private static readonly Dictionary<string, (Action<WorldSnapshot> Corrupt, string Error)> Corruptions = new()
    {
        ["version"] = (s => s.Version = 2, "unsupported version 2"),
        ["tick"] = (s => s.Tick = -1, "tick must not be negative"),
        ["rng"] = (s => s.RngIncrement = 2, "rngIncrement must be odd"),
        ["missing player"] = (s => s.Player = null, "player is missing"),
        ["class"] = (s => s.Player!.ClassId = "bard", "unknown class 'bard'"),
        ["stat"] = (s => s.Player!.Stats!.Strength = 999, "player strength must be between 20 and 100"),
        ["level"] = (s => s.Player!.Level = 0, "player level must be between 1 and 4"),
        ["experience"] = (s => s.Player!.Experience = -1, "player experience must not be negative"),
        ["gold"] = (s => s.Player!.Gold = -1, "player gold must not be negative"),
        ["null level"] = (s => s.Levels!.Add(null), "level is missing"),
        ["level twice"] = (s => s.Levels![1]!.Depth = 1, "level 1 appears twice"),
        ["fingerprint"] = (s => s.Levels![0]!.Fingerprint = "0", "does not match"),
        ["monster definition"] = (s => Living(s).Definition = "ghost", "unknown definition 'ghost'"),
        ["monster max life"] = (s => Living(s).MaxLife = 99, "maxLife is outside"),
        ["dead with life"] = (s => Corpse(s).Life = 5, "is dead but has life left"),
        ["shared cell"] = (s => { Living(s).X = Hero(s).X; Living(s).Y = Hero(s).Y; }, "shares cell"),
        ["wall"] = (s => { Hero(s).X = 0; Hero(s).Y = 0; }, "player stands on a wall"),
        ["player life"] = (s => Hero(s).Life = 0, "player life must be between 1"),
        ["facing"] = (s => Hero(s).Facing = "Up", "player facing 'Up' is not valid"),
        ["numeric enum"] = (s => Hero(s).Action = "3", "player action '3' is not valid"),
        ["elapsed"] = (s => Hero(s).ActionElapsed = 99, "player actionElapsed must be between 0"),
        ["null inventory"] = (s => s.Player!.Inventory = null, "player inventory is missing"),
        ["overlap"] = (s => { Stored(s, 1).X = Stored(s, 0).X; Stored(s, 1).Y = Stored(s, 0).Y; }, "overlaps another item"),
        ["duplicate item"] = (s => s.Player!.Belt![1]!.Item!.Id = s.Player.Belt[0]!.Item!.Id, "is used twice"),
        ["unknown base"] = (s => Stored(s, 0).Item!.Base = "sword", "unknown base 'sword'"),
        ["affix slot"] = (s => s.Player!.Belt![0]!.Item!.Prefix = new AffixRollSnapshot { Id = "keen", Value = 3 }, "cannot appear on this item"),
        ["affix value"] = (s => Worn(s, "Weapon").Prefix = new AffixRollSnapshot { Id = "keen", Value = 99 }, "value must be between 3 and 3"),
        ["armor"] = (s => Worn(s, "Body").Armor = 99, "armor must be between 5 and 5"),
        ["wrong slot"] = (s => s.Player!.Equipment!.First(e => e!.Slot == "Body")!.Slot = "Head", "cannot be worn as Head"),
        ["belt slot"] = (s => s.Player!.Belt![0]!.Slot = 8, "needs an empty slot"),
        ["ground gold"] = (s => GroundGold(s).Gold = 0, "gold must be between 1"),
        ["next item id"] = (s => s.NextItemId = 1, "nextItemId must be greater than every item id"),
        ["next actor id"] = (s => s.NextActorId = 2, "nextActorId must be greater than every actor id"),
        ["huge tick"] = (s => s.Tick = long.MaxValue, "tick is too large"),
        ["huge gold"] = (s => s.Player!.Gold = long.MaxValue, "player gold is too large"),
        ["huge experience"] = (s => s.Player!.Experience = long.MaxValue, "player experience is too large"),
        ["huge next actor id"] = (s => s.NextActorId = int.MaxValue, "nextActorId is too large"),
        ["huge next item id"] = (s => s.NextItemId = int.MaxValue, "nextItemId is too large"),
        ["experience below level"] = (s => { s.Player!.Level = 2; s.Player.Experience = 99; }, "player experience must be between 100 and 299 for level 2"),
        ["experience above level"] = (s => { s.Player!.Level = 1; s.Player.Experience = 100; }, "player experience must be between 0 and 99 for level 1"),
        ["padded enum"] = (s => Hero(s).Facing = " North", "player facing ' North' is not valid"),
        ["enum list"] = (s => Hero(s).Facing = "North, East", "player facing 'North, East' is not valid"),
    };

    public static IEnumerable<object[]> CorruptionNames => Corruptions.Keys.Select(name => new object[] { name });

    [Fact]
    public void RoundTrip_KeepsTheSameHash()
    {
        var world = BusyWorld();
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(Restore(WorldSnapshot.Capture(world))));
    }

    [Fact]
    public void RestoredWorld_PlaysOnIdentically()
    {
        var world = BusyWorld();
        var restored = Restore(WorldSnapshot.Capture(world));
        Assert.Equal(Play(world), Play(restored));
        Assert.Equal(WorldHash.Compute(world), WorldHash.Compute(restored));
    }

    [Theory]
    [MemberData(nameof(CorruptionNames))]
    public void CorruptSave_IsRejectedWithAClearError(string name)
    {
        var snapshot = WorldSnapshot.Capture(BusyWorld());
        var (corrupt, error) = Corruptions[name];
        corrupt(snapshot);
        var result = World.Restore(snapshot, Content, Generator());
        Assert.False(result.Succeeded);
        Assert.StartsWith("save is invalid: ", result.Error);
        Assert.Contains(error, result.Error);
    }

    [Fact]
    public void NullSnapshot_IsRejected()
    {
        Assert.Equal("save is empty", World.Restore(null!, Content, Generator()).Error);
    }

    private static World BusyWorld()
    {
        var world = TestWorlds.Create(Content);
        TestWorlds.Give(world, TestGameContent.Weapon());
        TestWorlds.Give(world, TestGameContent.Weapon());
        Assert.Null(LevelEntry.Enter(world, 2, MarkerKinds.Entrance));
        Assert.Null(LevelEntry.Enter(world, 1, MarkerKinds.Exit));
        var victim = TestWorlds.Spawn(world, "zombie", 6, 4);
        victim.Life = 1;
        TestWorlds.Spawn(world, "zombie", 2, 2);
        TestWorlds.Run(world, 30, new AttackCommand(victim.Id));
        Assert.True(victim.IsDead);
        var gold = TestWorlds.DropGold(world, 1, 4, 9);
        world.Tick(new Command[] { new PickUpCommand(gold.Id) });
        return world;
    }

    private static World Restore(WorldSnapshot snapshot)
    {
        var result = World.Restore(snapshot, Content, Generator());
        return result.World ?? throw new InvalidOperationException(result.Error);
    }

    private static FixedLevelGenerator Generator() => new FixedLevelGenerator(TestWorlds.Room, TestWorlds.Room);

    private static List<string> Play(World world) => TestWorlds.Run(world, 200).Select(e => e.ToString()).ToList();

    private static ActorSnapshot Hero(WorldSnapshot s) => s.Player!.Actor!;

    private static LevelSnapshot FirstLevel(WorldSnapshot s) => s.Levels!.First(level => level!.Depth == 1)!;

    private static ActorSnapshot Living(WorldSnapshot s) => FirstLevel(s).Monsters!.First(m => !m!.Dead)!;

    private static ActorSnapshot Corpse(WorldSnapshot s) => FirstLevel(s).Monsters!.First(m => m!.Dead)!;

    private static GroundItemSnapshot GroundGold(WorldSnapshot s) => FirstLevel(s).GroundItems!.First(g => g!.Item is null)!;

    private static StoredItemSnapshot Stored(WorldSnapshot s, int index) => s.Player!.Inventory![index]!;

    private static ItemSnapshot Worn(WorldSnapshot s, string slot) => s.Player!.Equipment!.First(e => e!.Slot == slot)!.Item!;
}
