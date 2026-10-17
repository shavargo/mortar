using Mortar.Core.Authoring;
using Mortar.Game.Content;

namespace Mortar.Tests.Gameplay.Support;

internal static class TestGameContent
{
    public static GameRules Rules(
        int minHit = 5,
        int maxHit = 95,
        int recoveryPercent = 20,
        long[]? thresholds = null,
        int experiencePercentPerLevel = 10,
        int minimumExperiencePercent = 0,
        int vendorStockRegenTicks = 144000,
        int vendorSellPercent = 25,
        int vendorMagicBuyMult = 3,
        int vendorRepairPercent = 50,
        int vendorIdentifyFee = 50,
        int vendorHealFee = 0,
        int vendorSmithSlots = 8,
        int vendorHealerSlots = 6,
        int vendorSageSlots = 6,
        int vendorSmithMagicPercent = 40) =>
        new GameRules(
            minHit, maxHit, 50, 1, 1, 2, 5, 10, recoveryPercent,
            experiencePercentPerLevel, minimumExperiencePercent, thresholds ?? new long[] { 100, 300, 600 },
            vendorStockRegenTicks: vendorStockRegenTicks,
            vendorSellPercent: vendorSellPercent,
            vendorMagicBuyMult: vendorMagicBuyMult,
            vendorRepairPercent: vendorRepairPercent,
            vendorIdentifyFee: vendorIdentifyFee,
            vendorHealFee: vendorHealFee,
            vendorSmithSlots: vendorSmithSlots,
            vendorHealerSlots: vendorHealerSlots,
            vendorSageSlots: vendorSageSlots,
            vendorSmithMagicPercent: vendorSmithMagicPercent);

    public static ActionTimings Timings(int move = 4, int attack = 6, int hit = 3, int recovery = 3) =>
        new ActionTimings(move, attack, hit, recovery);

    public static ClassDefinition Warrior(params string[] startingItems) => new ClassDefinition(
        "warrior",
        "Warrior",
        "warrior",
        new Stats(20, 20, 20, 10),
        new Stats(100, 100, 100, 100),
        new ClassGrowth(10, 2, 2, 0, 1, 1, 5),
        new IntRange(2, 4),
        Timings(),
        startingItems);

    public static MonsterDefinition Monster(
        string id,
        string ai = AiKinds.MeleeChase,
        MinionSpec? minions = null,
        int life = 10,
        int sight = 6,
        int experience = 50,
        string? loot = null,
        int preferredRange = 0) =>
        new MonsterDefinition(
            id, id, id, 1, new IntRange(life, life), 0, 50, new IntRange(1, 2), 0, Timings(), sight, experience, ai, minions, loot,
            preferredRange);

    public static LevelDefinition Level(int depth, string monsterId = "zombie", string? bossId = null) =>
        new LevelDefinition(depth, "test", new[] { new SpawnEntry(monsterId, 1) }, bossId);

    public static LevelDefinition Town(string areaId = "town") =>
        new LevelDefinition(0, areaId, Array.Empty<SpawnEntry>(), null);

    public static LevelDefinition[] WithTown() => new[] { Town(), Level(1), Level(2) };

    public static ItemBaseDefinition Weapon(
        string id = "knife", int damageMin = 3, int damageMax = 5, int strength = 0, WeaponSwing? swing = null,
        int itemLevel = 1, int dropWeight = 1, int value = 50, int maxDurability = 30) =>
        new ItemBaseDefinition(
            id, id, id, ItemSlots.Weapon, 1, 2, itemLevel, dropWeight, new Stats(strength, 0, 0, 0),
            new IntRange(damageMin, damageMax), swing, null, 0, null, value: value, maxDurability: maxDurability);

    public static ItemBaseDefinition Armor(
        string id = "vest", string slot = ItemSlots.Body, int armor = 5, int width = 2, int height = 2,
        int blockChance = 0, int dropWeight = 1, int value = 40, int maxDurability = 40) =>
        new ItemBaseDefinition(
            id, id, id, slot, width, height, 1, dropWeight, default, null, null, new IntRange(armor, armor), blockChance, null,
            value: value, maxDurability: maxDurability);

    public static ItemBaseDefinition Ring(string id = "ring", int dropWeight = 1, int value = 75, int maxDurability = 20) =>
        new ItemBaseDefinition(
            id, id, id, ItemSlots.Ring, 1, 1, 1, dropWeight, default, null, null, null, 0, null,
            value: value, maxDurability: maxDurability);

    public static ItemBaseDefinition Potion(
        string id = "tonic", int amount = 20, string restores = PotionKinds.Life, int dropWeight = 1,
        int value = 40, int maxDurability = 0) =>
        new ItemBaseDefinition(
            id, id, id, ItemSlots.Potion, 1, 1, 1, dropWeight, default, null, null, null, 0, new PotionEffect(restores, amount),
            value: value, maxDurability: maxDurability);

    public static AffixDefinition Affix(string id, string position, string bonus, int value, params string[] slots) =>
        new AffixDefinition(id, id, position, slots, new IntRange(1, 10), 1, bonus, new IntRange(value, value));

    public static UniquePower Power(string affixId, int value) => new UniquePower(affixId, value);

    public static UniqueDefinition Unique(
        string id = "relic",
        string name = "Relic",
        string baseItemId = "knife",
        int minLevel = 1,
        IReadOnlyList<UniquePower>? powers = null,
        int value = 0) =>
        new UniqueDefinition(id, name, baseItemId, minLevel, powers ?? new[] { Power("keen", 3) }, value);

    public static LootTableDefinition Loot(
        string id = "loot",
        int rolls = 1,
        int none = 0,
        int gold = 0,
        int item = 1,
        int magicPercent = 0,
        int uniqueChance = 0) =>
        new LootTableDefinition(id, rolls, none, gold, item, new IntRange(5, 5), magicPercent, uniqueChance);

    public static ItemCatalog Items(
        IReadOnlyList<ItemBaseDefinition>? bases = null,
        IReadOnlyList<AffixDefinition>? affixes = null,
        IReadOnlyList<LootTableDefinition>? loot = null,
        IReadOnlyList<UniqueDefinition>? uniques = null) =>
        new ItemCatalog(
            bases ?? Array.Empty<ItemBaseDefinition>(),
            affixes ?? Array.Empty<AffixDefinition>(),
            loot ?? Array.Empty<LootTableDefinition>(),
            uniques ?? Array.Empty<UniqueDefinition>());

    public static AuthoringResult<ContentSet> Create(
        IReadOnlyList<MonsterDefinition>? monsters = null,
        IReadOnlyList<LevelDefinition>? levels = null,
        GameRules? rules = null,
        IReadOnlyList<ClassDefinition>? classes = null,
        ItemCatalog? items = null) =>
        ContentSet.Create(
            rules ?? Rules(),
            classes ?? new[] { Warrior() },
            monsters ?? new[] { Monster("zombie") },
            levels ?? new[] { Level(1), Level(2) },
            items ?? ItemCatalog.Empty);

    public static ContentSet Valid(
        IReadOnlyList<MonsterDefinition>? monsters = null,
        IReadOnlyList<LevelDefinition>? levels = null,
        GameRules? rules = null,
        ItemCatalog? items = null,
        IReadOnlyList<ClassDefinition>? classes = null)
    {
        var result = Create(monsters, levels, rules, classes, items);
        return result.Value ?? throw new InvalidOperationException(string.Join("; ", result.Errors));
    }
}
