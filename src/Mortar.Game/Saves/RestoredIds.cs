namespace Mortar.Game.Saves;

internal sealed class RestoredIds
{
    private readonly HashSet<int> _actors = new HashSet<int>();
    private readonly HashSet<int> _items = new HashSet<int>();
    private readonly HashSet<int> _projectiles = new HashSet<int>();

    public int MaxActorId { get; private set; }

    public int MaxItemId { get; private set; }

    public int MaxProjectileId { get; private set; }

    public void ClaimActor(int id)
    {
        Check.That(id >= 1, "actor ids must be positive");
        Check.That(_actors.Add(id), $"actor id {id} is used twice");
        MaxActorId = Math.Max(MaxActorId, id);
    }

    public void ClaimItem(int id)
    {
        Check.That(id >= 1, "item ids must be positive");
        Check.That(_items.Add(id), $"item id {id} is used twice");
        MaxItemId = Math.Max(MaxItemId, id);
    }

    public void ClaimProjectile(int id)
    {
        Check.That(id >= 1, "projectile ids must be positive");
        Check.That(_projectiles.Add(id), $"projectile id {id} is used twice");
        MaxProjectileId = Math.Max(MaxProjectileId, id);
    }
}
