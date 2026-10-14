using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class AiSystem
{
    public static void Update(World world, EventSink events)
    {
        var level = world.Level;
        foreach (var monster in level.Actors)
        {
            if (IsActiveMonster(monster) && !monster.IsAwake && ShouldWake(world, monster))
            {
                WakeGroup(level, monster, events);
            }
        }
        foreach (var monster in level.Actors)
        {
            if (IsActiveMonster(monster) && monster.IsAwake && monster.Intent != IntentKind.Attack)
            {
                monster.SetAttackIntent(world.Player.Id);
            }
        }
    }

    private static bool IsActiveMonster(Actor actor) => actor.Kind == ActorKind.Monster && actor.IsAlive;

    private static bool ShouldWake(World world, Actor monster)
    {
        if (monster.LastAttackerId != 0) return true;
        var sight = world.Content.Monster(monster.DefinitionId).SightRadius;
        return monster.Cell.DistanceTo(world.Player.Cell) <= sight;
    }

    private static void WakeGroup(LevelState level, Actor monster, EventSink events)
    {
        var groupId = monster.LeaderId != 0 ? monster.LeaderId : monster.Id;
        foreach (var member in level.Actors)
        {
            if (!IsActiveMonster(member) || member.IsAwake) continue;
            if (member.Id == monster.Id || member.Id == groupId || member.LeaderId == groupId)
            {
                member.IsAwake = true;
                events.Add(new MonsterAwoke(events.Tick, member.Id));
            }
        }
    }
}
