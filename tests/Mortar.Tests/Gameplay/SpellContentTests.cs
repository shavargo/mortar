using Mortar.Game.Content;
using Mortar.Tests.Gameplay.Support;
using Xunit;

namespace Mortar.Tests.Gameplay;

public sealed class SpellContentTests
{
    [Fact]
    public void ProjectileSpellsNeedRange()
    {
        var spell = new SpellDefinition(
            "bolt", "Bolt", SpellShapes.Projectile, 1, 4, 2, 0, new IntRange(1, 2), 0, 0, 0, 0);

        var result = ContentSet.Create(
            TestGameContent.Rules(),
            new[] { TestGameContent.Warrior() },
            new[] { TestGameContent.Monster("zombie") },
            new[] { TestGameContent.Level(1) },
            ItemCatalog.Empty,
            new SpellCatalog(new[] { spell }));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("range", StringComparison.Ordinal));
    }

    [Fact]
    public void RangedWeaponsNeedAmmoAndAmmoMustExist()
    {
        var bow = new ItemBaseDefinition(
            "bow", "bow", "bow", ItemSlots.Weapon, 1, 2, 1, 1, default,
            new IntRange(2, 4), new WeaponSwing(8, 4), null, 0, null,
            ranged: true, ammoId: "arrows", stackMax: 1);

        var result = ContentSet.Create(
            TestGameContent.Rules(),
            new[] { TestGameContent.Warrior() },
            new[] { TestGameContent.Monster("zombie") },
            new[] { TestGameContent.Level(1) },
            TestGameContent.Items(bases: new[] { bow }));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Message.Contains("ammo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidBowAndAmmoPass()
    {
        var arrows = new ItemBaseDefinition(
            "arrows", "Arrows", "arrows", ItemSlots.Ammo, 1, 1, 1, 1, default,
            null, null, null, 0, null, stackMax: 40);
        var bow = new ItemBaseDefinition(
            "bow", "Bow", "bow", ItemSlots.Weapon, 1, 2, 1, 1, default,
            new IntRange(2, 5), new WeaponSwing(10, 5), null, 0, null,
            ranged: true, ammoId: "arrows", range: 8, value: 80, maxDurability: 30);

        var result = ContentSet.Create(
            TestGameContent.Rules(),
            new[] { TestGameContent.Warrior() },
            new[] { TestGameContent.Monster("zombie") },
            new[] { TestGameContent.Level(1) },
            TestGameContent.Items(bases: new[] { bow, arrows }));

        Assert.True(result.Succeeded, string.Join("; ", result.Errors));
    }
}
