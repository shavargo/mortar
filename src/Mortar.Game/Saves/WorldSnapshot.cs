using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

/// <summary>Plain data for a whole world. Values read from a file may be missing or invalid; <see cref="World.Restore"/> checks all of them.</summary>
public sealed class WorldSnapshot
{
    public const int CurrentVersion = 2;

    public int Version { get; set; }

    public uint Seed { get; set; }

    public long Tick { get; set; }

    public int Depth { get; set; }

    public int NextActorId { get; set; }

    public int NextItemId { get; set; }

    public int NextProjectileId { get; set; } = 1;

    public ulong RngState { get; set; }

    public ulong RngIncrement { get; set; }

    public PortalSnapshot? Portal { get; set; }

    public PlayerSnapshot? Player { get; set; }

    public List<LevelSnapshot?>? Levels { get; set; }

    public static WorldSnapshot Capture(World world) => SnapshotCapture.Capture(world);
}

public sealed class PortalSnapshot
{
    public int DungeonDepth { get; set; }

    public int DungeonX { get; set; }

    public int DungeonY { get; set; }

    public int TownX { get; set; }

    public int TownY { get; set; }
}

public sealed class KnownSpellSnapshot
{
    public string? Id { get; set; }

    public int Level { get; set; }
}

public sealed class ProjectileSnapshot
{
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public string? Kind { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int AimX { get; set; }

    public int AimY { get; set; }

    public int RemainingRange { get; set; }

    public int DamageMin { get; set; }

    public int DamageMax { get; set; }

    public bool HitsMonsters { get; set; }

    public bool HitsPlayer { get; set; }

    public int Radius { get; set; }
}

public sealed class PlayerSnapshot
{
    public string? ClassId { get; set; }

    public int Level { get; set; }

    public long Experience { get; set; }

    public StatsSnapshot? Stats { get; set; }

    public int UnspentPoints { get; set; }

    public long Gold { get; set; }

    public ActorSnapshot? Actor { get; set; }

    public List<StoredItemSnapshot?>? Inventory { get; set; }

    public List<BeltItemSnapshot?>? Belt { get; set; }

    public List<EquippedItemSnapshot?>? Equipment { get; set; }

    public List<KnownSpellSnapshot?>? Spells { get; set; }
}

public sealed class StatsSnapshot
{
    public int Strength { get; set; }

    public int Dexterity { get; set; }

    public int Vitality { get; set; }

    public int Magic { get; set; }
}

public sealed class LevelSnapshot
{
    public int Depth { get; set; }

    public uint Seed { get; set; }

    public string? Fingerprint { get; set; }

    public List<ActorSnapshot?>? Monsters { get; set; }

    public List<GroundItemSnapshot?>? GroundItems { get; set; }

    public List<ProjectileSnapshot?>? Projectiles { get; set; }
}

public sealed class ActorSnapshot
{
    public int Id { get; set; }

    public string? Definition { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public string? Facing { get; set; }

    public int Life { get; set; }

    public int Mana { get; set; }

    public int MaxLife { get; set; }

    public bool Dead { get; set; }

    public bool Awake { get; set; }

    public int LeaderId { get; set; }

    public int LastAttackerId { get; set; }

    public string? Action { get; set; }

    public int ActionElapsed { get; set; }

    public int ActionDuration { get; set; }

    public int ActionTargetId { get; set; }

    public string? ActionSpellId { get; set; }

    public int ActionAimX { get; set; }

    public int ActionAimY { get; set; }

    public int ActionItemId { get; set; }

    public string? Intent { get; set; }

    public int IntentX { get; set; }

    public int IntentY { get; set; }

    public int IntentTargetId { get; set; }

    public string? IntentSpellId { get; set; }

    public bool IntentFromStaff { get; set; }

    public int IntentItemId { get; set; }
}
