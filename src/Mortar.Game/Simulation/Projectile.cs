namespace Mortar.Game.Simulation;

public sealed class Projectile
{
    internal Projectile(
        int id,
        int ownerId,
        string kind,
        Cell cell,
        Cell aim,
        int remainingRange,
        int damageMin,
        int damageMax,
        bool hitsMonsters,
        bool hitsPlayer)
    {
        Id = id;
        OwnerId = ownerId;
        Kind = kind;
        Cell = cell;
        Aim = aim;
        RemainingRange = remainingRange;
        DamageMin = damageMin;
        DamageMax = damageMax;
        HitsMonsters = hitsMonsters;
        HitsPlayer = hitsPlayer;
    }

    public int Id { get; }

    public int OwnerId { get; }

    public string Kind { get; }

    public Cell Cell { get; internal set; }

    public Cell Aim { get; }

    public int RemainingRange { get; internal set; }

    public int DamageMin { get; }

    public int DamageMax { get; }

    public bool HitsMonsters { get; }

    public bool HitsPlayer { get; }
}
