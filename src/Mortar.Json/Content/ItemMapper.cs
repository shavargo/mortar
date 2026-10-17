using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Json.Content;

internal static class ItemMapper
{
    public static ItemBaseDefinition ToItemBase(string source, ItemBaseDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors);
        var size = fields.Present(dto.Size, "size") ? dto.Size! : new SizeDto { Width = 0, Height = 0 };
        return new ItemBaseDefinition(
            fields.Text(dto.Id, "id"),
            fields.Text(dto.Name, "name"),
            fields.Text(dto.Icon, "icon"),
            fields.Text(dto.Slot, "slot"),
            fields.Int(size.Width, "size.width"),
            fields.Int(size.Height, "size.height"),
            fields.Int(dto.ItemLevel, "itemLevel"),
            fields.Int(dto.DropWeight, "dropWeight"),
            ToRequirements(dto.Requirements),
            dto.Damage is null ? null : (IntRange?)fields.ReadRange(dto.Damage, "damage"),
            ToSwing(fields, dto.Swing),
            dto.Armor is null ? null : (IntRange?)fields.ReadRange(dto.Armor, "armor"),
            dto.BlockChance ?? 0,
            ToPotion(fields, dto.Potion),
            dto.Ranged ?? false,
            string.IsNullOrWhiteSpace(dto.Ammo) ? null : dto.Ammo,
            dto.StackMax ?? 1,
            string.IsNullOrWhiteSpace(dto.Spell) ? null : dto.Spell,
            dto.MaxCharges ?? 0,
            dto.Range ?? 0,
            dto.Value ?? 0,
            dto.MaxDurability ?? 0);
    }

    public static AffixDefinition ToAffix(string source, int index, AffixDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors, $"affixes entry {index}");
        return new AffixDefinition(
            fields.Text(dto.Id, "id"),
            fields.Text(dto.Name, "name"),
            fields.Text(dto.Position, "position"),
            ToSlots(fields, dto.Slots),
            fields.ReadRange(dto.ItemLevel, "itemLevel"),
            fields.Int(dto.Weight, "weight"),
            fields.Text(dto.Bonus, "bonus"),
            fields.ReadRange(dto.Value, "value"));
    }

    public static LootTableDefinition ToLootTable(string source, int index, LootTableDto dto, List<AuthoringError> errors)
    {
        var fields = new Fields(source, errors, $"tables entry {index}");
        return new LootTableDefinition(
            fields.Text(dto.Id, "id"),
            fields.Int(dto.Rolls, "rolls"),
            dto.NoneWeight ?? 0,
            dto.GoldWeight ?? 0,
            dto.ItemWeight ?? 0,
            dto.Gold is null ? default : fields.ReadRange(dto.Gold, "gold"),
            dto.MagicPercent ?? 0);
    }

    private static Stats ToRequirements(StatsDto? dto) =>
        dto is null ? default : new Stats(dto.Strength ?? 0, dto.Dexterity ?? 0, dto.Vitality ?? 0, dto.Magic ?? 0);

    private static WeaponSwing? ToSwing(Fields fields, SwingDto? dto) =>
        dto is null ? null : new WeaponSwing(fields.Int(dto.AttackTicks, "swing.attackTicks"), fields.Int(dto.HitFrame, "swing.hitFrame"));

    private static PotionEffect? ToPotion(Fields fields, PotionDto? dto) =>
        dto is null ? null : new PotionEffect(fields.Text(dto.Restores, "potion.restores"), fields.Int(dto.Amount, "potion.amount"));

    private static IReadOnlyList<string> ToSlots(Fields fields, List<string?>? slots) =>
        fields.Present(slots, "slots")
            ? slots!.Select((slot, i) => fields.Text(slot, $"slots[{i}]")).ToList()
            : new List<string>();
}
