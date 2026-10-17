namespace Mortar.Json.Content;

internal sealed class SizeDto
{
    public int? Width { get; set; }

    public int? Height { get; set; }
}

internal sealed class SwingDto
{
    public int? AttackTicks { get; set; }

    public int? HitFrame { get; set; }
}

internal sealed class PotionDto
{
    public string? Restores { get; set; }

    public int? Amount { get; set; }
}

internal sealed class ItemBaseDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public string? Slot { get; set; }

    public SizeDto? Size { get; set; }

    public int? ItemLevel { get; set; }

    public int? DropWeight { get; set; }

    public StatsDto? Requirements { get; set; }

    public RangeDto? Damage { get; set; }

    public SwingDto? Swing { get; set; }

    public RangeDto? Armor { get; set; }

    public int? BlockChance { get; set; }

    public PotionDto? Potion { get; set; }

    public bool? Ranged { get; set; }

    public string? Ammo { get; set; }

    public int? StackMax { get; set; }

    public string? Spell { get; set; }

    public int? MaxCharges { get; set; }

    public int? Range { get; set; }

    public int? Value { get; set; }

    public int? MaxDurability { get; set; }
}

internal sealed class AffixListDto
{
    public List<AffixDto?>? Affixes { get; set; }
}

internal sealed class AffixDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Position { get; set; }

    public List<string?>? Slots { get; set; }

    public RangeDto? ItemLevel { get; set; }

    public int? Weight { get; set; }

    public string? Bonus { get; set; }

    public RangeDto? Value { get; set; }
}

internal sealed class LootListDto
{
    public List<LootTableDto?>? Tables { get; set; }
}

internal sealed class LootTableDto
{
    public string? Id { get; set; }

    public int? Rolls { get; set; }

    public int? NoneWeight { get; set; }

    public int? GoldWeight { get; set; }

    public int? ItemWeight { get; set; }

    public RangeDto? Gold { get; set; }

    public int? MagicPercent { get; set; }

    public int? UniqueChance { get; set; }
}

internal sealed class UniqueListDto
{
    public List<UniqueDto?>? Uniques { get; set; }
}

internal sealed class UniqueDto
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? BaseItem { get; set; }

    public int? MinLevel { get; set; }

    public List<UniquePowerDto?>? Powers { get; set; }

    public int? Value { get; set; }
}

internal sealed class UniquePowerDto
{
    public string? Affix { get; set; }

    public int? Value { get; set; }
}
