using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Pathing;

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
            if (!IsActiveMonster(monster) || !monster.IsAwake || monster.Action != ActionKind.Idle) continue;
            ChooseIntent(world, monster);
        }
    }

    private static void ChooseIntent(World world, Actor monster)
    {
        var definition = world.Content.Monster(monster.DefinitionId);
        if (definition.Ai == AiKinds.RangedKite)
        {
            monster.ClearIntent();
            RangedKite(world, monster, definition.PreferredRange);
            return;
        }
        if (monster.Intent == IntentKind.None) monster.SetAttackIntent(world.Player.Id);
    }

    private static void RangedKite(World world, Actor monster, int preferredRange)
    {
        var player = world.Player;
        var distance = monster.Cell.DistanceTo(player.Cell);
        if (distance <= 1)
        {
            if (TryStepAway(world, monster, player.Cell)) return;
        }
        if (distance <= preferredRange)
        {
            monster.SetAttackIntent(player.Id);
            return;
        }
        monster.SetAttackIntent(player.Id);
    }

    private static bool TryStepAway(World world, Actor monster, Cell from)
    {
        Cell? best = null;
        var bestDistance = monster.Cell.DistanceTo(from);
        foreach (var direction in Directions.All)
        {
            var next = monster.Cell.Step(direction);
            if (!world.Level.IsFree(next)) continue;
            if (!Reach.CornersClear(monster.Cell, next, world.Level.IsWalkable)) continue;
            var distance = next.DistanceTo(from);
            if (distance > bestDistance)
            {
                bestDistance = distance;
                best = next;
            }
        }
        if (best is null) return false;
        monster.SetMoveIntent(best.Value);
        return true;
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
