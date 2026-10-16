using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Simulation;
using Mortar.Tests.Gameplay.Support;

namespace Mortar.Tests.Gameplay;

public class SpellItemTests
{
    private static SpellCatalog Spells() => new SpellCatalog(new[]
    {
        new SpellDefinition("firebolt", "Firebolt", SpellShapes.Projectile, 6, 6, 3, 8, new IntRange(4, 4), 0, 0, 2, 0),
        new SpellDefinition("healing", "Healing", SpellShapes.Self, 10, 6, 3, 0, null, 20, 0, 0, 5),
        new SpellDefinition("fireball", "Fireball", SpellShapes.Area, 18, 6, 3, 5, new IntRange(8, 8), 0, 2, 3, 0),
    });

    private static ItemBaseDefinition Book() => new ItemBaseDefinition(
        "book-fireball", "Book", "book", ItemSlots.Book, 1, 1, 1, 0, default,
        null, null, null, 0, null, spellId: "fireball");

    private static ItemBaseDefinition Scroll() => new ItemBaseDefinition(
        "scroll-heal", "Scroll", "scroll", ItemSlots.Scroll, 1, 1, 1, 0, default,
        null, null, null, 0, null, spellId: "healing");

    private static ItemBaseDefinition Staff() => new ItemBaseDefinition(
        "staff", "Staff", "staff", ItemSlots.Weapon, 1, 3, 1, 0, default,
        new IntRange(1, 2), new WeaponSwing(8, 4), null, 0, null,
        spellId: "firebolt", maxCharges: 5);

    private static World WorldWith(params ItemBaseDefinition[] extras)
    {
        var bases = new List<ItemBaseDefinition>(extras);
        var result = ContentSet.Create(
            TestGameContent.Rules(minHit: 100, maxHit: 100),
            new[] { TestGameContent.Warrior() },
            new[] { TestGameContent.Monster("zombie", sight: 1, life: 30) },
            new[] { TestGameContent.Level(1), TestGameContent.Level(2) },
            TestGameContent.Items(bases: bases.ToArray()),
            Spells());
        return TestWorlds.Create(result.Value!);
    }

    [Fact]
    public void Book_TeachesSpell()
    {
        var world = WorldWith(Book());
        var book = TestWorlds.Give(world, Book());
        Assert.False(world.Progress.Knows("fireball"));
        var events = world.Tick(new Command[] { new UseItemCommand(book.Id) });
        Assert.Contains(events.OfType<SpellLearned>(), e => e.SpellId == "fireball" && e.Level == 1);
        Assert.True(world.Progress.Knows("fireball"));
        Assert.Null(world.Belongings.Find(book.Id));
    }

    [Fact]
    public void Scroll_HealsWithoutManaOrLearning()
    {
        var world = WorldWith(Scroll());
        var scroll = TestWorlds.Give(world, Scroll());
        world.Player.Life = 5;
        world.Player.Mana = 0;
        var events = world.Tick(new Command[] { new UseItemCommand(scroll.Id) });
        Assert.Contains(events.OfType<SpellHealed>(), e => e.Amount > 0);
        Assert.False(world.Progress.Knows("healing"));
        Assert.Equal(0, world.Player.Mana);
        Assert.Null(world.Belongings.Find(scroll.Id));
    }

    [Fact]
    public void Staff_CastsWithoutLearningUsingCharges()
    {
        var world = WorldWith(Staff());
        var staff = ItemFactory.CreatePlain(world, Staff());
        world.Belongings.Equipment.Set(EquipmentSlot.Weapon, staff);
        world.RefreshPlayer();
        Assert.Equal(5, staff.Charges);
        var zombie = TestWorlds.Spawn(world, "zombie", 3, 1);
        var events = TestWorlds.Run(world, 4, new CastCommand("firebolt", zombie.Id));
        Assert.Equal(4, staff.Charges);
        Assert.Contains(events.OfType<ProjectileSpawned>(), e => e.Kind == "firebolt");
        Assert.False(world.Progress.Knows("firebolt"));
    }

    [Fact]
    public void Fireball_ExplodesAndDamagesNearbyMonsters()
    {
        var world = WorldWith();
        world.Progress.Learn("fireball", 15);
        world.Player.Mana = 50;
        var a = TestWorlds.Spawn(world, "zombie", 3, 1);
        var b = TestWorlds.Spawn(world, "zombie", 4, 1);
        var lifeA = a.Life;
        var lifeB = b.Life;
        var events = TestWorlds.Run(world, 10, new CastCommand("fireball", a.Id));
        Assert.Contains(events.OfType<ProjectileExploded>(), _ => true);
        Assert.True(a.Life < lifeA);
        Assert.True(b.Life < lifeB);
    }
}
