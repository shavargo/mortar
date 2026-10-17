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
            definition.IsStackable ? definition.StackMax : 1,
            definition.MaxCharges);

    public static Item? Roll(World world, int itemLevel, int magicPercent, int uniqueChance = 0)
    {
        var bases = world.Content.ItemBases.Where(b => b.DropWeight > 0 && b.ItemLevel <= itemLevel).ToList();
        return RollFrom(world, itemLevel, magicPercent, bases, uniqueChance);
    }

    public static Item? RollFrom(
        World world,
        int itemLevel,
        int magicPercent,
        IReadOnlyList<ItemBaseDefinition> bases,
        int uniqueChance = 0)
    {
        if (bases.Count == 0) return null;
        var definition = bases[world.Rng.PickWeighted(bases.Select(b => b.DropWeight).ToList())];
        var armor = definition.Armor is { } range ? world.Rng.NextInt(range.Min, range.Max + 1) : 0;
        var unique = TryPromoteUnique(world, definition, itemLevel, armor, uniqueChance);
        if (unique is not null) return unique;
        return CreateWithMagic(world, definition, itemLevel, armor, magicPercent);
    }

    private static Item? TryPromoteUnique(
        World world, ItemBaseDefinition definition, int itemLevel, int armor, int uniqueChance)
    {
        if (uniqueChance <= 0 || world.Rng.NextInt(100) >= uniqueChance) return null;
        var picked = PickEligibleUnique(world, definition.Id, itemLevel);
        if (picked is null) return null;
        world.MarkUniqueDropped(picked.Id);
        return CreateUnique(world, definition, itemLevel, armor, picked);
    }

    private static UniqueDefinition? PickEligibleUnique(World world, string baseItemId, int itemLevel)
    {
        var eligible = new List<UniqueDefinition>();
        foreach (var unique in world.Content.Uniques)
        {
            if (unique.BaseItemId != baseItemId) continue;
            if (unique.MinLevel > itemLevel) continue;
            if (world.WasUniqueDropped(unique.Id)) continue;
            eligible.Add(unique);
        }
        return eligible.Count == 0 ? null : world.Rng.Pick(eligible);
    }

    private static Item CreateUnique(
        World world, ItemBaseDefinition definition, int itemLevel, int armor, UniqueDefinition unique) =>
        new Item(
            world.NextItemId++,
            definition,
            itemLevel,
            armor,
            null,
            null,
            definition.IsStackable ? definition.StackMax : 1,
            definition.MaxCharges,
            identified: false,
            uniqueId: unique.Id,
            uniqueName: unique.Name,
            uniquePowers: BakePowers(world, unique),
            uniqueValue: unique.Value);

    private static IReadOnlyList<RolledAffix> BakePowers(World world, UniqueDefinition unique)
    {
        var powers = new List<RolledAffix>(unique.Powers.Count);
        foreach (var power in unique.Powers)
        {
            if (!world.Content.TryGetAffix(power.AffixId, out var affix)) continue;
            powers.Add(new RolledAffix(affix, power.Value));
        }
        return powers;
    }

    private static Item CreateWithMagic(
        World world, ItemBaseDefinition definition, int itemLevel, int armor, int magicPercent)
    {
        RolledAffix? prefix = null;
        RolledAffix? suffix = null;
        if (!definition.IsPotion && !definition.IsAmmo && world.Rng.NextInt(100) < magicPercent)
        {
            var shape = world.Rng.NextInt(3);
            if (shape != 1) prefix = RollAffix(world, definition, itemLevel, AffixPositions.Prefix);
            if (shape != 0) suffix = RollAffix(world, definition, itemLevel, AffixPositions.Suffix);
        }
        var stack = definition.IsStackable ? definition.StackMax : 1;
        var magic = prefix is not null || suffix is not null;
        return new Item(
            world.NextItemId++, definition, itemLevel, armor, prefix, suffix, stack, definition.MaxCharges,
            identified: !magic);
    }

    private static RolledAffix? RollAffix(World world, ItemBaseDefinition definition, int itemLevel, string position)
    {
        var affixes = world.Content.Affixes.Where(a => a.Position == position && a.Allows(definition.Slot, itemLevel)).ToList();
        if (affixes.Count == 0) return null;
        var affix = affixes[world.Rng.PickWeighted(affixes.Select(a => a.Weight).ToList())];
        return new RolledAffix(affix, world.Rng.NextInt(affix.Value.Min, affix.Value.Max + 1));
    }
}
