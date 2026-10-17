using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class SpellItemActions
{
    public static string? Use(World world, UseItemCommand command, EventSink events)
    {
        var item = world.Belongings.Find(command.ItemId);
        if (item is null) return "no such item";
        if (item.Definition.IsBook) return ReadBook(world, item, events);
        if (item.Definition.IsScroll) return ReadScroll(world, item, command, events);
        return "that item cannot be used";
    }

    private static string? ReadBook(World world, Item item, EventSink events)
    {
        var spellId = item.Definition.SpellId!;
        if (!world.Content.TryGetSpell(spellId, out _)) return "unknown spell";
        if (!world.Progress.Learn(spellId, world.Content.Rules.MaxSpellLevel))
        {
            return "spell is already at maximum level";
        }
        Consume(world, item, events);
        events.Add(new SpellLearned(events.Tick, spellId, world.Progress.SpellLevel(spellId)));
        return null;
    }

    private static string? ReadScroll(World world, Item item, UseItemCommand command, EventSink events)
    {
        var spellId = item.Definition.SpellId!;
        if (!world.Content.TryGetSpell(spellId, out var spell)) return "unknown spell";
        var aim = world.Player.Cell;
        var targetId = 0;
        if (spell!.Shape != SpellShapes.Self)
        {
            var aimError = ResolveAim(world, command, out aim, out targetId);
            if (aimError is not null) return aimError;
        }
        if (spell.Id == "identify")
        {
            if (command.TargetItemId == 0) return "identify needs an item";
            var error = SpellSystem.IdentifyItem(world, command.TargetItemId, events);
            if (error is not null) return error;
            Consume(world, item, events);
            return null;
        }
        if (spell.Id == "town-portal")
        {
            if (world.Content.ShallowestLevel > 0) return "there is no town";
            if (world.Depth <= 0) return "cannot open a portal in town";
        }
        var applyError = SpellSystem.ApplyScroll(world, world.Player, spell, aim, command.TargetItemId, events);
        if (applyError is not null) return applyError;
        world.Player.ClearAction();
        Consume(world, item, events);
        return null;
    }

    private static string? ResolveAim(World world, UseItemCommand command, out Cell aim, out int targetId)
    {
        aim = world.Player.Cell;
        targetId = 0;
        if (command.TargetActorId != 0)
        {
            var target = world.Level.Find(command.TargetActorId);
            if (target is null || !target.IsAlive || target.Kind != ActorKind.Monster)
            {
                return "no such monster on this level";
            }
            aim = target.Cell;
            targetId = target.Id;
            return null;
        }
        if (command.TargetCell is { } cell)
        {
            if (!world.Level.InBounds(cell)) return "invalid target";
            aim = cell;
            return null;
        }
        return "spell needs a target";
    }

    private static void Consume(World world, Item item, EventSink events)
    {
        world.Belongings.Take(item.Id, out _);
        events.Add(new ItemConsumed(events.Tick, item.Id, item.Definition.Id));
    }
}
