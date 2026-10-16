using Mortar.Game.Content;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class SpellSystem
{
    public static void StartCast(World world, Actor caster, SpellDefinition spell, int targetId, Cell aim, EventSink events)
    {
        caster.Facing = aim != caster.Cell ? Directions.Toward(caster.Cell, aim) : caster.Facing;
        caster.StartAction(ActionKind.Casting, spell.CastTicks, targetId);
        caster.ActionSpellId = spell.Id;
        caster.ActionAim = aim;
        caster.Mana -= spell.ManaCost;
        events.Add(new SpellCastStarted(events.Tick, caster.Id, spell.Id, spell.CastTicks));
    }

    public static void Resolve(World world, IReadOnlyList<Actor> due, EventSink events)
    {
        foreach (var caster in due)
        {
            if (!caster.IsAlive || caster.Action != ActionKind.Casting) continue;
            if (!world.Content.TryGetSpell(caster.ActionSpellId, out var spell)) continue;
            var level = world.Progress.SpellLevel(spell!.Id);
            Apply(world, caster, spell, level, events);
        }
    }

    private static void Apply(World world, Actor caster, SpellDefinition spell, int spellLevel, EventSink events)
    {
        switch (spell.Shape)
        {
            case SpellShapes.Self:
                Heal(caster, spell, spellLevel, events);
                break;
            case SpellShapes.Projectile:
                Bolt(world, caster, spell, spellLevel, events);
                break;
        }
    }

    private static void Heal(Actor caster, SpellDefinition spell, int spellLevel, EventSink events)
    {
        var amount = spell.HealAmount + spell.HealPerLevel * Math.Max(0, spellLevel - 1);
        var before = caster.Life;
        caster.Life = Math.Min(caster.Profile.MaxLife, caster.Life + amount);
        events.Add(new SpellHealed(events.Tick, caster.Id, spell.Id, caster.Life - before, caster.Life));
    }

    private static void Bolt(World world, Actor caster, SpellDefinition spell, int spellLevel, EventSink events)
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
            events);
    }
}
