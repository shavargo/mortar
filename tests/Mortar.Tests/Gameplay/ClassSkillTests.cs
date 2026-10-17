using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Tests.Support;

namespace Mortar.Tests.Gameplay;

public class ClassSkillTests
{
    [Fact]
    public void RogueDisarm_RejectsWithNoTrap()
    {
        var loaded = GameContentLoader.Load(ContentFixture.Manifest);
        var world = World.Create(loaded.Value!.Content, loaded.Value.CreateLevelGenerator(), "rogue", 1).World!;
        var events = world.Tick(new Command[] { new ClassSkillCommand() });
        Assert.Equal("no trap here", Assert.Single(events.OfType<CommandRejected>()).Reason);
    }

    [Fact]
    public void SorcererRecharge_RestoresStaffCharges()
    {
        var loaded = GameContentLoader.Load(ContentFixture.Manifest);
        var world = World.Create(loaded.Value!.Content, loaded.Value.CreateLevelGenerator(), "sorcerer", 1).World!;
        var staff = world.Belongings.Equipment.Get(EquipmentSlot.Weapon)!;
        Assert.Equal("oak-staff", staff.Definition.Id);
        staff.Charges = 0;
        var mana = world.Player.Mana;
        var events = world.Tick(new Command[] { new ClassSkillCommand() });
        Assert.Equal(staff.Definition.MaxCharges, staff.Charges);
        Assert.Equal(mana - staff.Definition.MaxCharges, world.Player.Mana);
        Assert.Contains(events.OfType<StaffRecharged>(), e => e.Charges == staff.Charges);
    }
}
