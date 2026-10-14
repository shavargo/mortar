namespace Mortar.Game.Simulation;

public sealed class Actor
{
    public const int PlayerId = 1;

    internal Actor(int id, ActorKind kind, string definitionId, CombatProfile profile)
    {
        Id = id;
        Kind = kind;
        DefinitionId = definitionId;
        Profile = profile;
        Life = profile.MaxLife;
        Mana = profile.MaxMana;
    }

    public int Id { get; }

    public ActorKind Kind { get; }

    public string DefinitionId { get; }

    public CombatProfile Profile { get; private set; }

    public Cell Cell { get; internal set; }

    public Direction Facing { get; internal set; }

    public int Life { get; internal set; }

    public int Mana { get; internal set; }

    public bool IsDead { get; private set; }

    public bool IsAwake { get; internal set; }

    public int LeaderId { get; internal set; }

    public int LastAttackerId { get; internal set; }

    public ActionKind Action { get; private set; }

    public int ActionElapsed { get; internal set; }

    public int ActionDuration { get; private set; }

    public int ActionTargetId { get; private set; }

    public IntentKind Intent { get; private set; }

    public Cell IntentCell { get; private set; }

    public int IntentTargetId { get; private set; }

    public bool IsAlive => !IsDead && Life > 0;

    internal void StartAction(ActionKind kind, int duration, int targetId = 0)
    {
        Action = kind;
        ActionDuration = duration;
        ActionElapsed = 0;
        ActionTargetId = targetId;
    }

    internal void ClearAction() => StartAction(ActionKind.Idle, 0);

    internal void SetMoveIntent(Cell cell)
    {
        Intent = IntentKind.MoveTo;
        IntentCell = cell;
        IntentTargetId = 0;
    }

    internal void SetAttackIntent(int targetId)
    {
        Intent = IntentKind.Attack;
        IntentCell = default;
        IntentTargetId = targetId;
    }

    internal void SetPickUpIntent(int groundItemId, Cell cell)
    {
        Intent = IntentKind.PickUp;
        IntentCell = cell;
        IntentTargetId = groundItemId;
    }

    internal void ClearIntent()
    {
        Intent = IntentKind.None;
        IntentCell = default;
        IntentTargetId = 0;
    }

    internal void ApplyProfile(CombatProfile profile)
    {
        Life = Math.Max(1, Life + profile.MaxLife - Profile.MaxLife);
        Mana = Math.Max(0, Mana + profile.MaxMana - Profile.MaxMana);
        Profile = profile;
    }

    internal void RestoreVitals()
    {
        Life = Profile.MaxLife;
        Mana = Profile.MaxMana;
    }

    internal void MarkDead()
    {
        IsDead = true;
        Life = Math.Min(Life, 0);
        ClearAction();
        ClearIntent();
    }
}
