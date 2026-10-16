using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class SpellSystem
{
    public static void StartCast(
        World world, Actor caster, SpellDefinition spell, int targetId, Cell aim, EventSink events, bool fromStaff, int itemId = 0)
    {
        caster.Facing = aim != caster.Cell ? Directions.Toward(caster.Cell, aim) : caster.Facing;
        caster.StartAction(ActionKind.Casting, spell.CastTicks, targetId);
        caster.ActionSpellId = spell.Id;
        caster.ActionAim = aim;
        caster.ActionItemId = itemId;
        if (fromStaff)
        {
            var staff = CombatSystem.EquippedWeapon(world);
            if (staff is null || staff.Charges < 1) return;
            staff.Charges--;
        }
        else
        {
            caster.Mana -= spell.ManaCost;
        }
        events.Add(new SpellCastStarted(events.Tick, caster.Id, spell.Id, spell.CastTicks));
    }

    public static void Resolve(World world, IReadOnlyList<Actor> due, EventSink events)
    {
        foreach (var caster in due)
        {
            if (!caster.IsAlive || caster.Action != ActionKind.Casting) continue;
            if (!world.Content.TryGetSpell(caster.ActionSpellId, out var spell)) continue;
            var level = Math.Max(1, world.Progress.SpellLevel(spell!.Id));
            Apply(world, caster, spell, level, events);
        }
    }

    public static void ApplyScroll(World world, Actor caster, SpellDefinition spell, EventSink events) =>
        Apply(world, caster, spell, spellLevel: 1, events);

    internal static void Apply(World world, Actor caster, SpellDefinition spell, int spellLevel, EventSink events)
    {
        if (spell.Id == "identify")
        {
            Identify(world, caster, events);
            return;
        }
        switch (spell.Shape)
        {
            case SpellShapes.Self:
                Heal(caster, spell, spellLevel, events);
                break;
            case SpellShapes.Projectile:
                Bolt(world, caster, spell, spellLevel, events, radius: 0);
                break;
            case SpellShapes.Area:
                Bolt(world, caster, spell, spellLevel, events, spell.Radius);
                break;
        }
    }

    public static string? IdentifyItem(World world, int itemId, EventSink events)
    {
        var item = world.Belongings.Find(itemId);
        if (item is null) return "no such item";
        if (item.Identified) return "item is already identified";
        item.Identified = true;
        world.RefreshPlayer();
        events.Add(new ItemIdentified(events.Tick, item.Id, item.Name));
        return null;
    }

    private static void Identify(World world, Actor caster, EventSink events)
    {
        var itemId = caster.ActionItemId;
        IdentifyItem(world, itemId, events);
    }

    private static void Heal(Actor caster, SpellDefinition spell, int spellLevel, EventSink events)
    {
        var amount = spell.HealAmount + spell.HealPerLevel * Math.Max(0, spellLevel - 1);
        var before = caster.Life;
        caster.Life = Math.Min(caster.Profile.MaxLife, caster.Life + amount);
        events.Add(new SpellHealed(events.Tick, caster.Id, spell.Id, caster.Life - before, caster.Life));
    }

    private static void Bolt(World world, Actor caster, SpellDefinition spell, int spellLevel, EventSink events, int radius)
    {
        var bonus = spell.DamagePerLevel * Math.Max(0, spellLevel - 1);
        var damage = spell.Damage ?? new IntRange(0, 0);
        ProjectileSystem.Spawn(
            world,
            caster,
            spell.Id,
            caster.ActionAim,
            spell.Range,
            damage.Min + bonus,
            damage.Max + bonus,
            events,
            radius);
    }
}
