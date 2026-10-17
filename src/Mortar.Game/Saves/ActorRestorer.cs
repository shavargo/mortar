using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Game.Saves;

internal static class ActorRestorer
{
    public static void RestoreMonster(World world, LevelState level, ActorSnapshot snapshot, RestoredIds ids)
    {
        var where = $"monster {snapshot.Id}";
        Check.That(snapshot.Id != Actor.PlayerId, $"{where} uses the player's id");
        ids.ClaimActor(snapshot.Id);
        var definitionId = Check.Text(snapshot.Definition, $"{where} definition");
        Check.That(world.Content.TryGetMonster(definitionId, out var definition), $"{where} has unknown definition '{definitionId}'");
        Check.That(
            snapshot.MaxLife >= definition!.Life.Min && snapshot.MaxLife <= definition.Life.Max,
            $"{where} maxLife is outside its definition's life range");
        var actor = new Actor(snapshot.Id, ActorKind.Monster, definition.Id, CombatProfile.ForMonster(definition, snapshot.MaxLife));
        Place(level, actor, snapshot, where);
    }

    public static void Place(LevelState level, Actor actor, ActorSnapshot snapshot, string where)
    {
        var cell = new Cell(snapshot.X, snapshot.Y);
        Check.That(level.IsWalkable(cell), $"{where} stands on a wall or outside the level");
        Check.Range(snapshot.Mana, 0, actor.Profile.MaxMana, $"{where} mana");
        if (snapshot.Dead) Check.That(snapshot.Life <= 0, $"{where} is dead but has life left");
        else Check.Range(snapshot.Life, 1, actor.Profile.MaxLife, $"{where} life");
        ApplyState(level, actor, snapshot, where);
        actor.Life = snapshot.Life;
        actor.Mana = snapshot.Mana;
        if (snapshot.Dead)
        {
            level.AddCorpse(actor, cell);
            actor.MarkDead();
            return;
        }
        Check.That(level.IsFree(cell), $"{where} shares cell {cell} with another actor");
        level.Add(actor, cell);
    }

    private static void ApplyState(LevelState level, Actor actor, ActorSnapshot snapshot, string where)
    {
        actor.Facing = Check.EnumValue<Direction>(snapshot.Facing, $"{where} facing");
        actor.IsAwake = snapshot.Awake;
        actor.LeaderId = Check.Range(snapshot.LeaderId, 0, int.MaxValue, $"{where} leaderId");
        actor.LastAttackerId = Check.Range(snapshot.LastAttackerId, 0, int.MaxValue, $"{where} lastAttackerId");
        var action = Check.EnumValue<ActionKind>(snapshot.Action, $"{where} action");
        var duration = Check.Range(snapshot.ActionDuration, 0, DefinitionChecks.MaxValue, $"{where} actionDuration");
        actor.StartAction(action, duration, Check.Range(snapshot.ActionTargetId, 0, int.MaxValue, $"{where} actionTargetId"));
        actor.ActionElapsed = Check.Range(snapshot.ActionElapsed, 0, duration, $"{where} actionElapsed");
        if (action == ActionKind.Casting)
        {
            actor.ActionSpellId = snapshot.ActionSpellId ?? "";
            actor.ActionAim = new Cell(snapshot.ActionAimX, snapshot.ActionAimY);
            actor.ActionItemId = Check.Range(snapshot.ActionItemId, 0, int.MaxValue, $"{where} actionItemId");
        }
        var intent = Check.EnumValue<IntentKind>(snapshot.Intent, $"{where} intent");
        var intentCell = new Cell(snapshot.IntentX, snapshot.IntentY);
        var intentTarget = Check.Range(snapshot.IntentTargetId, 0, int.MaxValue, $"{where} intentTargetId");
        if (intent == IntentKind.MoveTo || intent == IntentKind.PickUp || intent == IntentKind.Cast)
        {
            Check.That(level.InBounds(intentCell), $"{where} intent cell is outside the level");
        }
        switch (intent)
        {
            case IntentKind.MoveTo: actor.SetMoveIntent(intentCell); break;
            case IntentKind.Attack: actor.SetAttackIntent(intentTarget); break;
            case IntentKind.Cast:
                actor.SetCastIntent(
                    snapshot.IntentSpellId ?? "",
                    intentTarget,
                    intentCell,
                    snapshot.IntentFromStaff,
                    Check.Range(snapshot.IntentItemId, 0, int.MaxValue, $"{where} intentItemId"));
                break;
            case IntentKind.PickUp: actor.SetPickUpIntent(intentTarget, intentCell); break;
            default: actor.ClearIntent(); break;
        }
    }
}
