using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class ClassSkillSystem
{
    public static string? Use(World world, EventSink events)
    {
        var skill = world.PlayerClass.ClassSkill;
        if (string.IsNullOrEmpty(skill)) return "no class skill";
        return skill switch
        {
            ClassSkills.Disarm => "no trap here",
            ClassSkills.Recharge => Recharge(world, events),
            _ => "unknown class skill",
        };
    }

    private static string? Recharge(World world, EventSink events)
    {
        var staff = CombatSystem.EquippedWeapon(world);
        if (staff is null || staff.Definition.MaxCharges < 1 || string.IsNullOrEmpty(staff.Definition.SpellId))
        {
            return "no staff equipped";
        }
        var missing = staff.Definition.MaxCharges - staff.Charges;
        if (missing <= 0) return "staff is already charged";
        if (world.Player.Mana < missing) return "not enough mana";
        world.Player.Mana -= missing;
        staff.Charges = staff.Definition.MaxCharges;
        events.Add(new StaffRecharged(events.Tick, staff.Id, staff.Charges, missing));
        return null;
    }
}
