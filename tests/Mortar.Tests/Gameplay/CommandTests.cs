using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class CommandTests
{
    [Fact]
    public void UnknownStat_IsRejectedWithoutChangingProgress()
    {
        var world = TestWorlds.Create();
        world.Progress.UnspentPoints = 1;
        var stats = world.Progress.BaseStats;
        var events = world.Tick(new Command[] { new SpendStatPointCommand((StatKind)9) });
        var rejected = Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Equal("unknown stat", rejected.Reason);
        Assert.Equal(1, world.Progress.UnspentPoints);
        Assert.Equal(stats, world.Progress.BaseStats);
    }

    [Fact]
    public void NullCommand_IsRejected()
    {
        var world = TestWorlds.Create();
        var events = world.Tick(new Command[] { null! });
        var rejected = Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Equal("missing command", rejected.Reason);
        Assert.Null(rejected.Command);
        Assert.Equal("1 rejected '(none)': missing command", rejected.ToString());
    }

    [Fact]
    public void NullCommand_AfterGameOver_IsRejected()
    {
        var world = TestWorlds.Create();
        world.Player.Life = 0;
        world.Tick(Array.Empty<Command>());
        var events = world.Tick(new Command[] { null! });
        var rejected = Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Equal("missing command", rejected.Reason);
        Assert.Equal("2 rejected '(none)': missing command", rejected.ToString());
    }
}
