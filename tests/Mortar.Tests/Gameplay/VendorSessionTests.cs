using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class VendorSessionTests
{
    [Fact]
    public void Talk_AtSmith_ArmsSession()
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, new Cell(2, 1));
        var events = world.Tick(new Command[] { new TalkCommand(VendorKinds.Smith) });
        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Equal(VendorKinds.Smith, world.ActiveVendor);
    }

    [Fact]
    public void Leave_ClearsSession()
    {
        var world = ArmedAtSmith();
        var events = world.Tick(new Command[] { new LeaveVendorCommand() });
        Assert.Empty(events.OfType<CommandRejected>());
        Assert.Null(world.ActiveVendor);
        Assert.Empty(events.OfType<VendorSessionEnded>());
    }

    [Fact]
    public void WalkingOutOfRange_ClearsSessionAndEmitsEvent()
    {
        var world = ArmedAtSmith();
        world.Level.Move(world.Player, new Cell(5, 1));
        var events = world.Tick(Array.Empty<Command>());
        Assert.Null(world.ActiveVendor);
        Assert.Equal(VendorKinds.Smith, Assert.Single(events.OfType<VendorSessionEnded>()).VendorKind);
    }

    [Fact]
    public void LeavingTown_ClearsSessionAndEmitsEvent()
    {
        var world = ArmedAtSmith();
        world.Level.Move(world.Player, new Cell(4, 1));
        var events = world.Tick(new Command[] { new UseStairsCommand() });
        Assert.Equal(1, world.Depth);
        Assert.Null(world.ActiveVendor);
        Assert.Equal(VendorKinds.Smith, Assert.Single(events.OfType<VendorSessionEnded>()).VendorKind);
    }

    [Fact]
    public void Talk_InDungeon_IsRejected()
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, new Cell(4, 1));
        world.Tick(new Command[] { new UseStairsCommand() });
        Assert.Equal(1, world.Depth);
        var events = world.Tick(new Command[] { new TalkCommand(VendorKinds.Smith) });
        Assert.Equal("not in town", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Null(world.ActiveVendor);
    }

    [Fact]
    public void Talk_NotAdjacent_IsRejected()
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, new Cell(5, 1));
        var events = world.Tick(new Command[] { new TalkCommand(VendorKinds.Smith) });
        Assert.Equal("no vendor within reach", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Null(world.ActiveVendor);
    }

    [Fact]
    public void Talk_UnknownVendor_IsRejected()
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, new Cell(2, 1));
        var events = world.Tick(new Command[] { new TalkCommand("wirt") });
        Assert.Equal("unknown vendor", Assert.Single(events.OfType<CommandRejected>()).Reason);
        Assert.Null(world.ActiveVendor);
    }

    [Fact]
    public void Talk_HealerAndSage_ArmMatchingVendors()
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, new Cell(3, 1));
        world.Tick(new Command[] { new TalkCommand(VendorKinds.Healer) });
        Assert.Equal(VendorKinds.Healer, world.ActiveVendor);

        world.Level.Move(world.Player, new Cell(4, 1));
        world.Tick(new Command[] { new TalkCommand(VendorKinds.Sage) });
        Assert.Equal(VendorKinds.Sage, world.ActiveVendor);
    }

    [Fact]
    public void VendorState_ExistsOnWorld()
    {
        var world = TestWorlds.CreateWithTown();
        Assert.Equal(VendorKinds.Smith, world.Smith.Kind);
        Assert.Equal(VendorKinds.Healer, world.Healer.Kind);
        Assert.Equal(VendorKinds.Sage, world.Sage.Kind);
        Assert.Empty(world.Smith.Stock);
        Assert.Empty(world.Healer.Stock);
        Assert.Empty(world.Sage.Stock);
        Assert.Equal(world.Content.Rules.VendorStockRegenTicks, world.NextStockRegenTick);
    }

    private static World ArmedAtSmith()
    {
        var world = TestWorlds.CreateWithTown();
        world.Level.Move(world.Player, new Cell(2, 1));
        world.Tick(new Command[] { new TalkCommand(VendorKinds.Smith) });
        Assert.Equal(VendorKinds.Smith, world.ActiveVendor);
        return world;
    }
}
