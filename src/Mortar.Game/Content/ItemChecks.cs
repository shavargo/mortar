using Mortar.Core.Authoring;

namespace Mortar.Game.Content;

internal static class ItemChecks
{
    public const int MaxItemWidth = 2;
    public const int MaxItemHeight = 3;
    public const int MaxLootRolls = 10;

    private const int MaxValue = DefinitionChecks.MaxValue;

    public static void Validate(
        ItemCatalog items,
        SpellCatalog spells,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        List<AuthoringError> errors)
    {
        ContentValidator.CheckUniqueIds(items.Bases.Select(b => b.Id), "item", errors);
        ContentValidator.CheckUniqueIds(items.Affixes.Select(a => a.Id), "affix", errors);
        ContentValidator.CheckUniqueIds(items.LootTables.Select(t => t.Id), "loot table", errors);
        foreach (var item in items.Bases) CheckBase(item, errors);
        foreach (var affix in items.Affixes) CheckAffix(affix, errors);
        foreach (var table in items.LootTables) CheckLootTable(table, errors);
        CheckWeightTotal(items.Bases.Select(b => (long)Math.Max(0, b.DropWeight)), "items", "dropWeight", errors);
        CheckWeightTotal(items.Affixes.Select(a => (long)Math.Max(0, a.Weight)), "affixes", "weight", errors);
        CheckReferences(items, spells, classes, monsters, errors);
    }

    private static void CheckBase(ItemBaseDefinition item, List<AuthoringError> errors)
    {
        var source = $"item '{item.Id}'";
        if (string.IsNullOrWhiteSpace(item.Name)) errors.Add(source, "name is required");
        if (string.IsNullOrWhiteSpace(item.IconId)) errors.Add(source, "icon is required");
        if (!ItemSlots.IsKnown(item.Slot))
        {
            errors.Add(source, $"unknown slot '{item.Slot}'");
            return;
        }
        CheckSize(source, item, errors);
        if (item.ItemLevel < 1 || item.ItemLevel > MaxValue) errors.Add(source, $"itemLevel must be between 1 and {MaxValue}");
        if (item.DropWeight < 0 || item.DropWeight > MaxValue) errors.Add(source, $"dropWeight must be between 0 and {MaxValue}");
        foreach (var kind in Stats.Kinds)
        {
            var value = item.Requirements.Get(kind);
            if (value < 0 || value > MaxValue)
            {
                errors.Add(source, $"{kind.ToString().ToLowerInvariant()} requirement must be between 0 and {MaxValue}");
            }
        }
        CheckWeapon(source, item, errors);
        CheckArmor(source, item, errors);
        CheckPotion(source, item, errors);
        CheckRangedAndStacks(source, item, errors);
        CheckSpellItem(source, item, errors);
    }

    private static void CheckSize(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        if (item.Width < 1 || item.Width > MaxItemWidth || item.Height < 1 || item.Height > MaxItemHeight)
        {
            errors.Add(source, $"size must be 1-{MaxItemWidth} wide and 1-{MaxItemHeight} tall");
        }
        var small = item.Slot == ItemSlots.Ring || item.Slot == ItemSlots.Amulet || item.Slot == ItemSlots.Potion
            || item.Slot == ItemSlots.Ammo || item.Slot == ItemSlots.Book || item.Slot == ItemSlots.Scroll;
        if (small && (item.Width != 1 || item.Height != 1))
        {
            errors.Add(source, "rings, amulets, potions, ammo, books, and scrolls must be 1x1");
        }
    }

    private static void CheckWeapon(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        var isWeapon = item.Slot == ItemSlots.Weapon;
        if (isWeapon && item.Damage is null) errors.Add(source, "weapons need damage");
        if (!isWeapon && (item.Damage is not null || item.Swing is not null)) errors.Add(source, "only weapons have damage or swing");
        if (item.Damage is { } damage) DefinitionChecks.CheckRange(source, "damage", damage, 1, errors);
        if (item.Swing is { } swing && (swing.HitFrame < 1 || swing.HitFrame >= swing.AttackTicks || swing.AttackTicks > MaxValue))
        {
            errors.Add(source, $"swing needs 1 <= hitFrame < attackTicks <= {MaxValue}");
        }
    }

    private static void CheckRangedAndStacks(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        if (item.StackMax < 1 || item.StackMax > MaxValue) errors.Add(source, $"stackMax must be between 1 and {MaxValue}");
        if (item.Slot == ItemSlots.Ammo && item.StackMax < 2) errors.Add(source, "ammo needs stackMax of at least 2");
        if (item.StackMax > 1 && item.Slot != ItemSlots.Ammo) errors.Add(source, "only ammo may stack");
        if (item.Ranged && item.Slot != ItemSlots.Weapon) errors.Add(source, "only weapons may be ranged");
        if (!item.Ranged && item.AmmoId is not null) errors.Add(source, "ammo is only set on ranged weapons");
        if (item.Ranged && string.IsNullOrWhiteSpace(item.AmmoId)) errors.Add(source, "ranged weapons need ammo");
        if (item.Range < 0 || item.Range > MaxValue) errors.Add(source, $"range must be between 0 and {MaxValue}");
        if (item.Ranged && item.Range < 1) errors.Add(source, "ranged weapons need range of at least 1");
        if (!item.Ranged && item.Range != 0) errors.Add(source, "only ranged weapons have range");
    }

    private static void CheckSpellItem(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        if (item.MaxCharges < 0 || item.MaxCharges > MaxValue) errors.Add(source, $"maxCharges must be between 0 and {MaxValue}");
        if (item.MaxCharges > 0 && string.IsNullOrWhiteSpace(item.SpellId))
        {
            errors.Add(source, "charged items need a spell");
        }
        if (item.MaxCharges > 0 && item.Slot != ItemSlots.Weapon)
        {
            errors.Add(source, "only weapons may hold staff charges");
        }
        if (item.Slot == ItemSlots.Weapon && item.SpellId is not null && item.MaxCharges < 1)
        {
            errors.Add(source, "weapons with a spell need maxCharges");
        }
        if ((item.IsBook || item.IsScroll) && string.IsNullOrWhiteSpace(item.SpellId))
        {
            errors.Add(source, "books and scrolls need a spell");
        }
        if ((item.IsBook || item.IsScroll) && item.MaxCharges != 0)
        {
            errors.Add(source, "books and scrolls do not hold charges");
        }
        if (item.SpellId is not null && item.Slot != ItemSlots.Weapon && !item.IsBook && !item.IsScroll)
        {
            errors.Add(source, "only weapons, books, and scrolls may reference a spell");
        }
    }

    private static void CheckArmor(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        var armored = item.Slot == ItemSlots.Body || item.Slot == ItemSlots.Head || item.Slot == ItemSlots.Shield;
        if (armored && item.Armor is null) errors.Add(source, "body, head, and shield items need armor");
        if (!armored && item.Armor is not null) errors.Add(source, "only body, head, and shield items have armor");
        if (item.Armor is { } armor) DefinitionChecks.CheckRange(source, "armor", armor, 0, errors);
        if (item.BlockChance < 0 || item.BlockChance > 100) errors.Add(source, "blockChance must be between 0 and 100");
        if (item.BlockChance != 0 && item.Slot != ItemSlots.Shield) errors.Add(source, "only shields have blockChance");
    }

    private static void CheckPotion(string source, ItemBaseDefinition item, List<AuthoringError> errors)
    {
        if (item.Slot != ItemSlots.Potion)
        {
            if (item.Potion is not null) errors.Add(source, "only potions have an effect");
            return;
        }
        if (item.Potion is null)
        {
            errors.Add(source, "potions need an effect");
            return;
        }
        if (!PotionKinds.IsKnown(item.Potion.Restores)) errors.Add(source, $"unknown potion effect '{item.Potion.Restores}'");
        if (item.Potion.Amount < 1 || item.Potion.Amount > MaxValue) errors.Add(source, $"potion amount must be between 1 and {MaxValue}");
    }

    private static void CheckAffix(AffixDefinition affix, List<AuthoringError> errors)
    {
        var source = $"affix '{affix.Id}'";
        if (string.IsNullOrWhiteSpace(affix.Name)) errors.Add(source, "name is required");
        if (!AffixPositions.IsKnown(affix.Position)) errors.Add(source, $"unknown position '{affix.Position}'");
        if (affix.Slots.Count == 0 || affix.Slots.Any(slot => !ItemSlots.IsEquippable(slot)))
        {
            errors.Add(source, "slots must list one or more equippable slots");
        }
        DefinitionChecks.CheckRange(source, "itemLevel", affix.ItemLevel, 1, errors);
        if (affix.Weight < 1 || affix.Weight > MaxValue) errors.Add(source, $"weight must be between 1 and {MaxValue}");
        if (!BonusKinds.IsKnown(affix.Bonus)) errors.Add(source, $"unknown bonus '{affix.Bonus}'");
        DefinitionChecks.CheckRange(source, "value", affix.Value, 1, errors);
    }

    private static void CheckLootTable(LootTableDefinition table, List<AuthoringError> errors)
    {
        var source = $"loot table '{table.Id}'";
        if (table.Rolls < 1 || table.Rolls > MaxLootRolls) errors.Add(source, $"rolls must be between 1 and {MaxLootRolls}");
        var weights = new[] { table.NoneWeight, table.GoldWeight, table.ItemWeight };
        if (weights.Any(weight => weight < 0 || weight > MaxValue)) errors.Add(source, $"weights must be between 0 and {MaxValue}");
        else if (weights.Sum() < 1) errors.Add(source, "noneWeight, goldWeight, and itemWeight must add up to at least 1");
        if (table.GoldWeight > 0) DefinitionChecks.CheckRange(source, "gold", table.Gold, 1, errors);
        if (table.MagicPercent < 0 || table.MagicPercent > 100) errors.Add(source, "magicPercent must be between 0 and 100");
    }

    private static void CheckWeightTotal(IEnumerable<long> weights, string source, string field, List<AuthoringError> errors)
    {
        if (weights.Sum() > int.MaxValue) errors.Add(source, $"{field} values add up to more than {int.MaxValue}");
    }

    private static void CheckReferences(
        ItemCatalog items,
        SpellCatalog spells,
        IReadOnlyList<ClassDefinition> classes,
        IReadOnlyList<MonsterDefinition> monsters,
        List<AuthoringError> errors)
    {
        var bases = new HashSet<string>(items.Bases.Select(b => b.Id), StringComparer.Ordinal);
        var tables = new HashSet<string>(items.LootTables.Select(t => t.Id), StringComparer.Ordinal);
        var spellIds = new HashSet<string>(spells.Spells.Select(s => s.Id), StringComparer.Ordinal);
        foreach (var definition in classes)
        {
            foreach (var id in definition.StartingItems)
            {
                if (!bases.Contains(id)) errors.Add($"class '{definition.Id}'", $"starting item references unknown item '{id}'");
            }
            foreach (var id in definition.StartingSpells)
            {
                if (!spellIds.Contains(id)) errors.Add($"class '{definition.Id}'", $"starting spell references unknown spell '{id}'");
            }
        }
        foreach (var monster in monsters)
        {
            if (monster.LootTableId is not null && !tables.Contains(monster.LootTableId))
            {
                errors.Add($"monster '{monster.Id}'", $"loot references unknown loot table '{monster.LootTableId}'");
            }
        }
        foreach (var item in items.Bases)
        {
            var source = $"item '{item.Id}'";
            if (item.AmmoId is not null && !bases.Contains(item.AmmoId))
            {
                errors.Add(source, $"ammo references unknown item '{item.AmmoId}'");
            }
            if (item.SpellId is not null && !spellIds.Contains(item.SpellId))
            {
                errors.Add(source, $"spell references unknown spell '{item.SpellId}'");
            }
        }
    }
}
