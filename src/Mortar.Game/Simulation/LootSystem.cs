using Mortar.Game.Events;
using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class LootSystem
{
    private const int GoldRoll = 1;
    private const int ItemRoll = 2;

    public static void Drop(World world, Actor monster, EventSink events)
    {
        var definition = world.Content.Monster(monster.DefinitionId);
        if (definition.LootTableId is null) return;
        var table = world.Content.LootTable(definition.LootTableId);
        var weights = new[] { table.NoneWeight, table.GoldWeight, table.ItemWeight };
        for (var roll = 0; roll < table.Rolls; roll++)
        {
            var outcome = world.Rng.PickWeighted(weights);
            if (outcome == GoldRoll)
            {
                Place(world, monster.Cell, null, world.Rng.NextInt(table.Gold.Min, table.Gold.Max + 1), events);
            }
            else if (outcome == ItemRoll)
            {
                var item = ItemFactory.Roll(world, definition.Level, table.MagicPercent);
                if (item is not null) Place(world, monster.Cell, item, 0, events);
            }
        }
    }

    public static void Place(World world, Cell origin, Item? item, int gold, EventSink events)
    {
        var level = world.Level;
        var ground = new GroundItem(item?.Id ?? world.NextItemId++, level.DropCell(origin), item, gold);
        level.AddGroundItem(ground);
        events.Add(new ItemDropped(events.Tick, ground.Id, ground.Cell, ground.Describe()));
    }
}
