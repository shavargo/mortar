using Mortar.Game;
using Mortar.Game.Commands;
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

    [Fact]
    public void WorldHash_ChangesWhenStateChanges()
    {
        var world = TestWorlds.Create();
        var before = WorldHash.Compute(world);
        world.Tick(new Command[] { new MoveCommand(new Cell(3, 1)) });
        Assert.NotEqual(before, WorldHash.Compute(world));
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
