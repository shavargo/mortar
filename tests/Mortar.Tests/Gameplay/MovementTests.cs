using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class MovementTests
{
    private static readonly Command[] None = Array.Empty<Command>();

    private static readonly string[] SplitRoom =
    {
        "#########",
        "#E..#...#",
        "#...#...#",
        "#...#..X#",
        "#########",
    };

    private static readonly string[] Corridor =
    {
        "###########",
        "#E.......X#",
        "###########",
    };

    [Fact]
    public void MoveCommand_StartsAStepImmediately()
    {
        var world = TestWorlds.Create();
        var events = world.Tick(new Command[] { new MoveCommand(new Cell(4, 1)) });
        var moved = Assert.IsType<ActorMoved>(Assert.Single(events));
        Assert.Equal(Actor.PlayerId, moved.ActorId);
        Assert.Equal(new Cell(1, 1), moved.From);
        Assert.Equal(new Cell(2, 1), moved.To);
        Assert.Equal(4, moved.DurationTicks);
        Assert.Equal(new Cell(2, 1), world.Player.Cell);
        Assert.Equal(Direction.East, world.Player.Facing);
        Assert.Equal(ActionKind.Moving, world.Player.Action);
    }

    [Fact]
    public void Walking_FollowsThePathUntilTheDestination()
    {
        var world = TestWorlds.Create();
        var moves = new List<ActorMoved>();
        moves.AddRange(world.Tick(new Command[] { new MoveCommand(new Cell(4, 1)) }).OfType<ActorMoved>());
        for (var i = 0; i < 12; i++)
        {
            moves.AddRange(world.Tick(None).OfType<ActorMoved>());
        }
        Assert.Equal(new long[] { 1, 5, 9 }, moves.Select(m => m.Tick));
        Assert.Equal(new Cell(4, 1), world.Player.Cell);
        Assert.Equal(ActionKind.Idle, world.Player.Action);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void MoveIntoAWall_IsRejected()
    {
        var world = TestWorlds.Create();
        var events = world.Tick(new Command[] { new MoveCommand(new Cell(0, 0)) });
        var rejected = Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Equal("destination is not walkable", rejected.Reason);
        Assert.Equal("1 rejected 'move to (0,0)': destination is not walkable", rejected.ToString());
    }

    [Fact]
    public void UnreachableDestination_IsRejected()
    {
        var world = TestWorlds.Create(levels: new[] { SplitRoom, SplitRoom });
        var events = world.Tick(new Command[] { new MoveCommand(new Cell(6, 1)) });
        var rejected = Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Equal("destination is unreachable", rejected.Reason);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void OccupiedDestination_StopsNextToTheOccupant()
    {
        var world = TestWorlds.Create(TestGameContent.Valid(monsters: new[] { TestGameContent.Monster("zombie", sight: 1) }));
        TestWorlds.Spawn(world, "zombie", 5, 1);
        var events = TestWorlds.Run(world, 14, new MoveCommand(new Cell(5, 1)));
        var moves = events.OfType<ActorMoved>().Where(m => m.ActorId == Actor.PlayerId).ToList();
        Assert.Equal(new long[] { 1, 5, 9 }, moves.Select(m => m.Tick));
        Assert.Equal(new Cell(4, 1), world.Player.Cell);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void BlockedCorridor_WaitsAndContinuesOnceClear()
    {
        var world = TestWorlds.Create(TestGameContent.Valid(monsters: new[] { TestGameContent.Monster("zombie", sight: 1) }), levels: new[] { Corridor, Corridor });
        var blocker = TestWorlds.Spawn(world, "zombie", 6, 1);
        var waiting = TestWorlds.Run(world, 5, new MoveCommand(new Cell(8, 1)));
        Assert.Empty(waiting.OfType<ActorMoved>());
        Assert.Equal(IntentKind.MoveTo, world.Player.Intent);
        Assert.Equal(ActionKind.Idle, world.Player.Action);
        world.Level.Remove(blocker);
        TestWorlds.Run(world, 30);
        Assert.Equal(new Cell(8, 1), world.Player.Cell);
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void WalkWithNoTerrainPath_IsAbandoned()
    {
        var world = TestWorlds.Create(levels: new[] { SplitRoom, SplitRoom });
        world.Player.SetMoveIntent(new Cell(6, 1));
        Assert.Empty(world.Tick(None));
        Assert.Equal(IntentKind.None, world.Player.Intent);
    }

    [Fact]
    public void NewDestination_TakesEffectAfterTheCurrentStep()
    {
        var world = TestWorlds.Create();
        world.Tick(new Command[] { new MoveCommand(new Cell(4, 1)) });
        var events = new List<GameEvent>();
        events.AddRange(world.Tick(new Command[] { new MoveCommand(new Cell(1, 4)) }));
        for (var i = 0; i < 3; i++)
        {
            events.AddRange(world.Tick(None));
        }
        var moved = Assert.IsType<ActorMoved>(Assert.Single(events));
        Assert.Equal(5, moved.Tick);
        Assert.Equal(new Cell(2, 1), moved.From);
        Assert.Equal(new Cell(2, 2), moved.To);
    }
}
