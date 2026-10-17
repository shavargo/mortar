using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class ClassSkillSystem
{
    public static string? Use(World world, ClassSkillCommand command, EventSink events)
    {
        var skill = world.PlayerClass.ClassSkill;
        if (string.IsNullOrEmpty(skill)) return "no class skill";
        return skill switch
        {
            ClassSkills.Disarm => "no trap here",
            ClassSkills.Recharge => Recharge(world, events),
            ClassSkills.Repair => Repair(world, command.ItemId, events),
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

    private static string? Repair(World world, int? itemId, EventSink events)
    {
        if (itemId is null) return "repair needs an item";
        var item = world.Belongings.Find(itemId.Value);
        if (item is null) return "no such item";
        var slot = world.Belongings.Equipment.SlotOf(item.Id);
        if (slot is null) return "item is not equipped";
        if (item.MaxDurability <= 0) return "item cannot be repaired";
        if (item.Durability >= item.MaxDurability) return "item does not need repair";
        return ApplyFieldRepair(world, item, slot.Value, events);
    }

    private static string? ApplyFieldRepair(World world, Item item, EquipmentSlot slot, EventSink events)
    {
        var level = Math.Max(1, world.Progress.Level);
        var max = item.MaxDurability;
        var durability = item.Durability;
        var rep = 0;
        do
        {
            rep += level + world.Rng.NextInt(level);
            max -= Math.Max(max / (level + 9), 1);
            if (max == 0)
            {
                DestroyEquipped(world, item, slot, events);
                return null;
            }
        }
        while (durability + rep < max);

        item.MaxDurability = max;
        item.Durability = Math.Min(durability + rep, max);
        world.RefreshPlayer();
        events.Add(new ItemFieldRepaired(events.Tick, item.Id, item.Durability, item.MaxDurability));
        return null;
    }

    private static void DestroyEquipped(World world, Item item, EquipmentSlot slot, EventSink events)
    {
        var name = item.Name;
        var id = item.Id;
        world.Belongings.Equipment.Set(slot, null);
        world.RefreshPlayer();
        events.Add(new ItemDestroyed(events.Tick, id, name));
    }
}
