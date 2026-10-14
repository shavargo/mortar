using Mortar.Game;
using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Tests.Gameplay.Support;
using Mortar.Tests.Support;

namespace Mortar.Tests.Gameplay;

public class DeterminismTests
{
    private const int RunTicks = 3000;

    private static GameContent Content()
    {
        var result = GameContentLoader.Load(ContentFixture.Manifest);
        return result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors));
    }

    [Fact]
    public void SameSeedAndCommands_GiveTheSameLogAndHash()
    {
        var content = Content();
        var first = ScriptedPlayer.Run(content, 7, RunTicks);
        var second = ScriptedPlayer.Run(content, 7, RunTicks);
        Assert.Equal(first.Log, second.Log);
        Assert.Equal(first.Hash, second.Hash);
    }

    [Fact]
    public void DifferentSeeds_GiveDifferentRuns()
    {
        var content = Content();
        Assert.NotEqual(ScriptedPlayer.Run(content, 7, 600).Hash, ScriptedPlayer.Run(content, 8, 600).Hash);
    }

    [Fact]
    public void ScriptedRun_KillsMonsters()
    {
        var run = ScriptedPlayer.Run(Content(), 7, RunTicks);
        Assert.Contains(run.Log, line => line.Contains("(killed by #1)"));
    }

    [Theory]
    [InlineData("rng")]
    [InlineData("monster life")]
    [InlineData("player intent")]
    [InlineData("unspent points")]
    [InlineData("next actor id")]
    [InlineData("gold")]
    [InlineData("item cell")]
    [InlineData("equipment")]
    [InlineData("belt")]
    [InlineData("ground item")]
    [InlineData("next item id")]
    public void WorldHash_ChangesWhenOnePieceOfStateChanges(string change)
    {
        var knife = TestGameContent.Weapon();
        var tonic = TestGameContent.Potion();
        var world = TestWorlds.Create(TestGameContent.Valid(items: TestGameContent.Items(new[] { knife, tonic })));
        var zombie = TestWorlds.Spawn(world, "zombie", 5, 3);
        var item = TestWorlds.Give(world, knife);
        var potion = TestWorlds.Give(world, tonic);
        var before = WorldHash.Compute(world);
        Assert.Equal(before, WorldHash.Compute(world));
        Mutate(world, zombie, item, potion, change);
        Assert.NotEqual(before, WorldHash.Compute(world));
    }

    [Fact]
    public void ScriptedRun_PicksUpLoot()
    {
        var run = ScriptedPlayer.Run(Content(), 7, RunTicks);
        Assert.Contains(run.Log, line => line.Contains("picked up"));
    }

    private static void Mutate(World world, Actor zombie, Item item, Item potion, string change)
    {
        var belongings = world.Belongings;
        switch (change)
        {
            case "rng": world.Rng.NextInt(100); break;
            case "monster life": zombie.Life--; break;
            case "player intent": world.Player.SetMoveIntent(new Cell(3, 1)); break;
            case "unspent points": world.Progress.UnspentPoints++; break;
            case "next actor id": world.NextActorId++; break;
            case "gold": belongings.Gold++; break;
            case "item cell":
                belongings.Inventory.Remove(item.Id);
                belongings.Inventory.TryPlace(item, new Cell(5, 0));
                break;
            case "equipment":
                belongings.Inventory.Remove(item.Id);
                belongings.Equipment.Set(EquipmentSlot.Weapon, item);
                break;
            case "belt":
                belongings.Belt.Take(0);
                belongings.Belt.TryPut(potion, 4);
                break;
            case "ground item": world.Level.AddGroundItem(new GroundItem(99, new Cell(3, 3), null, 5)); break;
            case "next item id": world.NextItemId++; break;
        }
    }

    [Fact]
    public void ScriptedRun_MatchesSnapshot()
    {
        var run = ScriptedPlayer.Run(Content(), 7, RunTicks);
        var actual = string.Join("\n", run.Log.Take(300)) + $"\nhash: {run.Hash}\n";
        var path = Path.Combine(TestPaths.Root, "tests", "Mortar.Tests", "Snapshots", "game-seed-7.txt");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            Assert.Fail($"Snapshot created at {path}; review it, then run the tests again.");
        }
        Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n"), actual);
    }
}
