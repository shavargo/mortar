using Mortar.Game.Content;
using Mortar.Game.Items;

namespace Mortar.Game.Saves;

internal static class ItemRestorer
{
    public static Item Restore(
        ContentSet content,
        ItemSnapshot snapshot,
        RestoredIds ids,
        string where,
        bool fromV1 = false,
        bool fromPreV3 = false,
        bool fromPreV4 = false)
    {
        ids.ClaimItem(snapshot.Id);
        var baseId = Check.Text(snapshot.Base, $"{where} base");
        Check.That(content.TryGetItemBase(baseId, out var definition), $"{where} has unknown base '{baseId}'");
        var itemLevel = Check.Range(snapshot.ItemLevel, 1, DefinitionChecks.MaxValue, $"{where} itemLevel");
        var armorRange = definition!.Armor ?? new IntRange(0, 0);
        var armor = Check.Range(snapshot.Armor, armorRange.Min, armorRange.Max, $"{where} armor");
        var unique = RestoreUnique(content, snapshot, definition, where, fromPreV4);
        var prefix = unique is null
            ? RestoreAffix(content, snapshot.Prefix, definition, itemLevel, AffixPositions.Prefix, $"{where} prefix")
            : null;
        var suffix = unique is null
            ? RestoreAffix(content, snapshot.Suffix, definition, itemLevel, AffixPositions.Suffix, $"{where} suffix")
            : null;
        var stackMax = definition.StackMax;
        var stackCount = snapshot.StackCount <= 0 ? 1 : Check.Range(snapshot.StackCount, 1, stackMax, $"{where} stackCount");
        var charges = fromV1 && definition.MaxCharges > 0
            ? definition.MaxCharges
            : Check.Range(snapshot.Charges, 0, definition.MaxCharges, $"{where} charges");
        var identified = fromV1 || snapshot.Identified;
        var item = new Item(
            snapshot.Id,
            definition,
            itemLevel,
            armor,
            prefix,
            suffix,
            stackCount,
            charges,
            identified,
            uniqueId: unique?.Id,
            uniqueName: unique?.Name,
            uniquePowers: unique is null ? null : BakePowers(content, unique),
            uniqueValue: unique?.Value ?? 0);
        if (!fromPreV3)
        {
            ApplyDurability(item, snapshot, definition, where, fromPreV4);
        }
        return item;
    }

    private static UniqueDefinition? RestoreUnique(
        ContentSet content, ItemSnapshot snapshot, ItemBaseDefinition definition, string where, bool fromPreV4)
    {
        if (fromPreV4 || string.IsNullOrWhiteSpace(snapshot.UniqueId)) return null;
        Check.That(content.TryGetUnique(snapshot.UniqueId, out var unique), $"{where} has unknown unique '{snapshot.UniqueId}'");
        Check.That(unique!.BaseItemId == definition.Id, $"{where} unique '{snapshot.UniqueId}' does not match base");
        return unique;
    }

    private static void ApplyDurability(
        Item item, ItemSnapshot snapshot, ItemBaseDefinition definition, string where, bool fromPreV4)
    {
        var maxDurability = ResolveMaxDurability(snapshot, definition, where, fromPreV4);
        item.MaxDurability = maxDurability;
        item.Durability = Check.Range(snapshot.Durability, 0, maxDurability, $"{where} durability");
    }

    private static int ResolveMaxDurability(
        ItemSnapshot snapshot, ItemBaseDefinition definition, string where, bool fromPreV4)
    {
        if (definition.MaxDurability == 0)
        {
            return fromPreV4 ? 0 : Check.Range(snapshot.MaxDurability, 0, 0, $"{where} maxDurability");
        }
        if (fromPreV4) return definition.MaxDurability;
        return Check.Range(snapshot.MaxDurability, 1, definition.MaxDurability, $"{where} maxDurability");
    }

    private static IReadOnlyList<RolledAffix> BakePowers(ContentSet content, UniqueDefinition unique)
    {
        var powers = new List<RolledAffix>(unique.Powers.Count);
        foreach (var power in unique.Powers)
        {
            if (!content.TryGetAffix(power.AffixId, out var affix)) continue;
            powers.Add(new RolledAffix(affix, power.Value));
        }
        return powers;
    }

    private static RolledAffix? RestoreAffix(
        ContentSet content, AffixRollSnapshot? snapshot, ItemBaseDefinition definition, int itemLevel, string position, string where)
    {
        if (snapshot is null) return null;
        var id = Check.Text(snapshot.Id, $"{where} id");
        Check.That(content.TryGetAffix(id, out var affix), $"{where} has unknown affix '{id}'");
        Check.That(affix!.Position == position && affix.Allows(definition.Slot, itemLevel), $"{where} '{id}' cannot appear on this item");
        return new RolledAffix(affix, Check.Range(snapshot.Value, affix.Value.Min, affix.Value.Max, $"{where} value"));
    }
}
