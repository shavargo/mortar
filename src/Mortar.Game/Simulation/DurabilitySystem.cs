using Mortar.Game.Items;

namespace Mortar.Game.Simulation;

internal static class DurabilitySystem
{
    public static void OnPlayerHitLanded(World world) =>
        Wear(world, world.Belongings.Equipment.Get(EquipmentSlot.Weapon));

    public static void OnPlayerDamaged(World world)
    {
        Wear(world, world.Belongings.Equipment.Get(EquipmentSlot.Body));
        Wear(world, world.Belongings.Equipment.Get(EquipmentSlot.Shield));
    }

    private static void Wear(World world, Item? item)
    {
        if (item is null || item.MaxDurability <= 0 || item.Durability <= 0) return;
        item.Durability--;
        if (item.Durability == 0) world.RefreshPlayer();
    }
}
