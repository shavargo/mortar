using Mortar.Game.Content;
using Mortar.Game.Items;

namespace Mortar.Game.Saves;

internal static class ItemRestorer
{
    public static Item Restore(ContentSet content, ItemSnapshot snapshot, RestoredIds ids, string where)
    {
        ids.ClaimItem(snapshot.Id);
        var baseId = Check.Text(snapshot.Base, $"{where} base");
        Check.That(content.TryGetItemBase(baseId, out var definition), $"{where} has unknown base '{baseId}'");
        var itemLevel = Check.Range(snapshot.ItemLevel, 1, DefinitionChecks.MaxValue, $"{where} itemLevel");
        var armorRange = definition!.Armor ?? new IntRange(0, 0);
        var armor = Check.Range(snapshot.Armor, armorRange.Min, armorRange.Max, $"{where} armor");
        var prefix = RestoreAffix(content, snapshot.Prefix, definition, itemLevel, AffixPositions.Prefix, $"{where} prefix");
        var suffix = RestoreAffix(content, snapshot.Suffix, definition, itemLevel, AffixPositions.Suffix, $"{where} suffix");
        var stackMax = definition.StackMax;
        var stackCount = snapshot.StackCount <= 0 ? 1 : Check.Range(snapshot.StackCount, 1, stackMax, $"{where} stackCount");
        var charges = Check.Range(snapshot.Charges, 0, Math.Max(definition.MaxCharges, snapshot.Charges), $"{where} charges");
        return new Item(snapshot.Id, definition, itemLevel, armor, prefix, suffix, stackCount, charges);
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
