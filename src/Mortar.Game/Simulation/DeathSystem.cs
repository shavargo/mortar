using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class DeathSystem
{
    public static void Process(World world, EventSink events)
    {
        var level = world.Level;
        foreach (var actor in level.Actors)
        {
            if (actor.IsDead || actor.Life > 0) continue;
            actor.MarkDead();
            level.Vacate(actor);
            events.Add(new ActorDied(events.Tick, actor.Id, actor.LastAttackerId));
            if (actor.Kind != ActorKind.Monster) continue;
            if (actor.LastAttackerId == Actor.PlayerId && !world.Player.IsDead)
            {
                PlayerGrowth.RewardKill(world, actor, events);
            }
            LootSystem.Drop(world, actor, events);
        }
    }
}
