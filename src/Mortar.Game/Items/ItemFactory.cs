using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Game.Items;

internal static class ItemFactory
{
    public static Item CreatePlain(World world, ItemBaseDefinition definition) =>
        new Item(
            world.NextItemId++,
            definition,
            definition.ItemLevel,
            definition.Armor?.Max ?? 0,
            null,
            null,
            definition.IsStackable ? definition.StackMax : 1);

    public static Item? Roll(World world, int itemLevel, int magicPercent)
    {
        var bases = world.Content.ItemBases.Where(b => b.DropWeight > 0 && b.ItemLevel <= itemLevel).ToList();
        if (bases.Count == 0) return null;
        var definition = bases[world.Rng.PickWeighted(bases.Select(b => b.DropWeight).ToList())];
        var armor = definition.Armor is { } range ? world.Rng.NextInt(range.Min, range.Max + 1) : 0;
        RolledAffix? prefix = null;
        RolledAffix? suffix = null;
        if (!definition.IsPotion && !definition.IsAmmo && world.Rng.NextInt(100) < magicPercent)
        {
            var shape = world.Rng.NextInt(3);
            if (shape != 1) prefix = RollAffix(world, definition, itemLevel, AffixPositions.Prefix);
            if (shape != 0) suffix = RollAffix(world, definition, itemLevel, AffixPositions.Suffix);
        }
        var stack = definition.IsStackable ? definition.StackMax : 1;
        return new Item(world.NextItemId++, definition, itemLevel, armor, prefix, suffix, stack);
    }

    private static RolledAffix? RollAffix(World world, ItemBaseDefinition definition, int itemLevel, string position)
    {
        var affixes = world.Content.Affixes.Where(a => a.Position == position && a.Allows(definition.Slot, itemLevel)).ToList();
        if (affixes.Count == 0) return null;
        var affix = affixes[world.Rng.PickWeighted(affixes.Select(a => a.Weight).ToList())];
        return new RolledAffix(affix, world.Rng.NextInt(affix.Value.Min, affix.Value.Max + 1));
    }
}
