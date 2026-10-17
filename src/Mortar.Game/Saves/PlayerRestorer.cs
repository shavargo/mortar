using Mortar.Core.Rng;
using Mortar.Game.Content;
using Mortar.Game.Items;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class PlayerRestorer
{
    public static World CreateWorld(WorldSnapshot snapshot, PlayerSnapshot player, ContentSet content, ILevelGenerator generator)
    {
        var classId = Check.Text(player.ClassId, "player classId");
        Check.That(content.TryGetClass(classId, out var definition), $"unknown class '{classId}'");
        var progress = RestoreProgress(player, definition!, content.Rules);
        var rng = Pcg32.Restore(snapshot.RngState, snapshot.RngIncrement);
        return World.ForRestore(content, generator, snapshot.Seed, definition!, progress, rng);
    }

    public static void Place(World world, PlayerSnapshot player, int depth, RestoredIds ids, bool fromV1, bool fromPreV3)
    {
        RestoreBelongings(world, player, ids, fromV1, fromPreV3);
        RestoreSpells(world, player);
        world.RefreshPlayer();
        var level = world.FindLevel(depth);
        Check.That(level is not null, $"current depth {depth} is not a saved level");
        var actor = Check.Present(player.Actor, "player actor");
        Check.That(actor.Id == Actor.PlayerId, $"player actor id must be {Actor.PlayerId}");
        ids.ClaimActor(actor.Id);
        Check.That(actor.Definition == world.PlayerClass.Id, "player actor definition must match the class");
        world.Depth = depth;
        ActorRestorer.Place(level!, world.Player, actor, "player");
    }

    private static void RestoreSpells(World world, PlayerSnapshot player)
    {
        foreach (var entry in player.Spells ?? Enumerable.Empty<KnownSpellSnapshot?>())
        {
            var spell = Check.Present(entry, "known spell");
            var id = Check.Text(spell.Id, "known spell id");
            Check.That(world.Content.TryGetSpell(id, out _), $"unknown spell '{id}'");
            var level = Check.Range(spell.Level, 1, world.Content.Rules.MaxSpellLevel, $"spell '{id}' level");
            world.Progress.SetSpell(id, level);
        }
    }

    private static PlayerProgress RestoreProgress(PlayerSnapshot player, ClassDefinition definition, GameRules rules)
    {
        var saved = Check.Present(player.Stats, "player stats");
        var stats = new Stats(saved.Strength, saved.Dexterity, saved.Vitality, saved.Magic);
        foreach (var kind in Stats.Kinds)
        {
            var name = $"player {kind.ToString().ToLowerInvariant()}";
            Check.Range(stats.Get(kind), definition.BaseStats.Get(kind), definition.StatCaps.Get(kind), name);
        }
        Check.Range(player.Level, 1, rules.MaxLevel, "player level");
        CheckExperience(player.Level, player.Experience, rules);
        Check.Range(player.UnspentPoints, 0, DefinitionChecks.MaxValue, "player unspentPoints");
        return new PlayerProgress(definition.Id, player.Level, player.Experience, stats, player.UnspentPoints);
    }

    private static void CheckExperience(int level, long experience, GameRules rules)
    {
        Check.That(experience >= 0, "player experience must not be negative");
        Check.That(experience <= SaveLimits.MaxTotal, "player experience is too large");
        var min = level == 1 ? 0 : rules.ExperienceThresholds[level - 2];
        var max = level == rules.MaxLevel ? SaveLimits.MaxTotal : rules.ExperienceThresholds[level - 1] - 1;
        Check.That(
            experience >= min && experience <= max,
            $"player experience must be between {min} and {max} for level {level}");
    }

    private static void RestoreBelongings(World world, PlayerSnapshot player, RestoredIds ids, bool fromV1, bool fromPreV3)
    {
        var belongings = world.Belongings;
        Check.That(player.Gold >= 0, "player gold must not be negative");
        Check.That(player.Gold <= SaveLimits.MaxTotal, "player gold is too large");
        belongings.Gold = player.Gold;
        foreach (var stored in Check.Present(player.Inventory, "player inventory"))
        {
            var entry = Check.Present(stored, "inventory entry");
            var item = ItemRestorer.Restore(
                world.Content, Check.Present(entry.Item, "inventory item"), ids, "inventory item", fromV1, fromPreV3);
            Check.That(
                belongings.Inventory.TryPlace(item, new Cell(entry.X, entry.Y)),
                $"inventory item {item.Id} overlaps another item or lies outside the grid");
        }
        foreach (var belted in Check.Present(player.Belt, "player belt"))
        {
            var entry = Check.Present(belted, "belt entry");
            var item = ItemRestorer.Restore(
                world.Content, Check.Present(entry.Item, "belt item"), ids, "belt item", fromV1, fromPreV3);
            Check.That(
                belongings.Belt.TryPut(item, entry.Slot),
                $"belt item {item.Id} must be a potion or ammo and needs an empty slot from 0 to {Belt.Slots - 1}");
        }
        foreach (var worn in Check.Present(player.Equipment, "player equipment"))
        {
            var entry = Check.Present(worn, "equipment entry");
            var slot = Check.EnumValue<EquipmentSlot>(entry.Slot, "equipment slot");
            var item = ItemRestorer.Restore(
                world.Content, Check.Present(entry.Item, $"{slot} item"), ids, $"{slot} item", fromV1, fromPreV3);
            Check.That(Equipment.Accepts(slot, item.Definition), $"item {item.Id} cannot be worn as {slot}");
            Check.That(belongings.Equipment.Get(slot) is null, $"{slot} is equipped twice");
            belongings.Equipment.Set(slot, item);
        }
    }
}
