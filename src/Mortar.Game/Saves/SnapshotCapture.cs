using Mortar.Game.Items;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class SnapshotCapture
{
    public static WorldSnapshot Capture(World world) => new WorldSnapshot
    {
        Version = WorldSnapshot.CurrentVersion,
        Seed = world.Seed,
        Tick = world.TickCount,
        Depth = world.Depth,
        NextActorId = world.NextActorId,
        NextItemId = world.NextItemId,
        RngState = world.Rng.State,
        RngIncrement = world.Rng.Increment,
        Player = CapturePlayer(world),
        Levels = world.VisitedDepths.Select(depth => (LevelSnapshot?)CaptureLevel(world.GetLevel(depth))).ToList(),
    };

    private static PlayerSnapshot CapturePlayer(World world)
    {
        var progress = world.Progress;
        var stats = progress.BaseStats;
        var belongings = world.Belongings;
        return new PlayerSnapshot
        {
            ClassId = world.PlayerClass.Id,
            Level = progress.Level,
            Experience = progress.Experience,
            Stats = new StatsSnapshot { Strength = stats.Strength, Dexterity = stats.Dexterity, Vitality = stats.Vitality, Magic = stats.Magic },
            UnspentPoints = progress.UnspentPoints,
            Gold = belongings.Gold,
            Actor = CaptureActor(world.Player),
            Inventory = belongings.Inventory.Items
                .Select(stored => (StoredItemSnapshot?)new StoredItemSnapshot { X = stored.Cell.X, Y = stored.Cell.Y, Item = CaptureItem(stored.Item) })
                .ToList(),
            Belt = Enumerable.Range(0, Belt.Slots)
                .Where(slot => belongings.Belt.Get(slot) is not null)
                .Select(slot => (BeltItemSnapshot?)new BeltItemSnapshot { Slot = slot, Item = CaptureItem(belongings.Belt.Get(slot)!) })
                .ToList(),
            Equipment = Equipment.Slots
                .Where(slot => belongings.Equipment.Get(slot) is not null)
                .Select(slot => (EquippedItemSnapshot?)new EquippedItemSnapshot { Slot = slot.ToString(), Item = CaptureItem(belongings.Equipment.Get(slot)!) })
                .ToList(),
        };
    }

    private static LevelSnapshot CaptureLevel(LevelState level) => new LevelSnapshot
    {
        Depth = level.Depth,
        Seed = level.Seed,
        Fingerprint = level.Layout.Fingerprint,
        Monsters = level.Actors.Where(actor => actor.Kind == ActorKind.Monster).Select(actor => (ActorSnapshot?)CaptureActor(actor)).ToList(),
        GroundItems = level.GroundItems.Select(ground => (GroundItemSnapshot?)new GroundItemSnapshot
        {
            Id = ground.Id,
            X = ground.Cell.X,
            Y = ground.Cell.Y,
            Gold = ground.Gold,
            Item = ground.Item is null ? null : CaptureItem(ground.Item),
        }).ToList(),
    };

    private static ActorSnapshot CaptureActor(Actor actor) => new ActorSnapshot
    {
        Id = actor.Id,
        Definition = actor.DefinitionId,
        X = actor.Cell.X,
        Y = actor.Cell.Y,
        Facing = actor.Facing.ToString(),
        Life = actor.Life,
        Mana = actor.Mana,
        MaxLife = actor.Profile.MaxLife,
        Dead = actor.IsDead,
        Awake = actor.IsAwake,
        LeaderId = actor.LeaderId,
        LastAttackerId = actor.LastAttackerId,
        Action = actor.Action.ToString(),
        ActionElapsed = actor.ActionElapsed,
        ActionDuration = actor.ActionDuration,
        ActionTargetId = actor.ActionTargetId,
        Intent = actor.Intent.ToString(),
        IntentX = actor.IntentCell.X,
        IntentY = actor.IntentCell.Y,
        IntentTargetId = actor.IntentTargetId,
    };

    private static ItemSnapshot CaptureItem(Item item) => new ItemSnapshot
    {
        Id = item.Id,
        Base = item.Definition.Id,
        ItemLevel = item.ItemLevel,
        Armor = item.ArmorValue,
        Prefix = CaptureAffix(item.Prefix),
        Suffix = CaptureAffix(item.Suffix),
    };

    private static AffixRollSnapshot? CaptureAffix(RolledAffix? affix) =>
        affix is null ? null : new AffixRollSnapshot { Id = affix.Definition.Id, Value = affix.Value };
}
