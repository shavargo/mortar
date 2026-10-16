namespace Mortar.Game.Events;

public sealed class ProjectileSpawned : GameEvent
{
    public ProjectileSpawned(long tick, int projectileId, int ownerId, string kind, Cell cell)
        : base(tick)
    {
        ProjectileId = projectileId;
        OwnerId = ownerId;
        Kind = kind;
        Cell = cell;
    }

    public int ProjectileId { get; }

    public int OwnerId { get; }

    public string Kind { get; }

    public Cell Cell { get; }

    protected override string Describe() => $"#{OwnerId} launched {Kind} #{ProjectileId} at {Cell}";
}

public sealed class ProjectileMoved : GameEvent
{
    public ProjectileMoved(long tick, int projectileId, Cell from, Cell to)
        : base(tick)
    {
        ProjectileId = projectileId;
        From = from;
        To = to;
    }

    public int ProjectileId { get; }

    public Cell From { get; }

    public Cell To { get; }

    protected override string Describe() => $"projectile #{ProjectileId} {From} -> {To}";
}

public sealed class ProjectileHit : GameEvent
{
    public ProjectileHit(long tick, int projectileId, int ownerId, int targetId, int damage)
        : base(tick)
    {
        ProjectileId = projectileId;
        OwnerId = ownerId;
        TargetId = targetId;
        Damage = damage;
    }

    public int ProjectileId { get; }

    public int OwnerId { get; }

    public int TargetId { get; }

    public int Damage { get; }

    protected override string Describe() => $"projectile #{ProjectileId} hit #{TargetId} for {Damage}";
}

public sealed class ProjectileStopped : GameEvent
{
    public ProjectileStopped(long tick, int projectileId, Cell cell, string reason)
        : base(tick)
    {
        ProjectileId = projectileId;
        Cell = cell;
        Reason = reason;
    }

    public int ProjectileId { get; }

    public Cell Cell { get; }

    public string Reason { get; }

    protected override string Describe() => $"projectile #{ProjectileId} stopped at {Cell} ({Reason})";
}

public sealed class ProjectileExploded : GameEvent
{
    public ProjectileExploded(long tick, int projectileId, Cell cell, int radius)
        : base(tick)
    {
        ProjectileId = projectileId;
        Cell = cell;
        Radius = radius;
    }

    public int ProjectileId { get; }

    public Cell Cell { get; }

    public int Radius { get; }

    protected override string Describe() => $"projectile #{ProjectileId} exploded at {Cell} r={Radius}";
}
