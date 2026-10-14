using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class StartingKit
{
    public static void Give(World world)
    {
        var belongings = world.Belongings;
        foreach (var id in world.PlayerClass.StartingItems)
        {
            var item = ItemFactory.CreatePlain(world, world.Content.ItemBase(id));
            if (item.Definition.IsEquippable)
            {
                var slot = belongings.Equipment.SlotFor(item.Definition);
                if (belongings.Equipment.Get(slot) is null)
                {
                    belongings.Equipment.Set(slot, item);
                    continue;
                }
            }
            belongings.Stow(item);
        }
    }
}
