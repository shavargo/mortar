using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Game.Items;

internal static class ItemFactory
{
    public static Item CreatePlain(World world, ItemBaseDefinition definition) =>
        new Item(world.NextItemId++, definition, definition.ItemLevel, definition.Armor?.Max ?? 0, null, null);
}
