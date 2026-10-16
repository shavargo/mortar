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

    public string ActionSpellId { get; internal set; } = "";

    public Cell ActionAim { get; internal set; }

    public int ActionItemId { get; internal set; }

    public IntentKind Intent { get; private set; }

    public Cell IntentCell { get; private set; }

    public int IntentTargetId { get; private set; }

    public string IntentSpellId { get; private set; } = "";

    public bool IntentFromStaff { get; private set; }

    public int IntentItemId { get; private set; }

    public bool IsAlive => !IsDead && Life > 0;

    internal void StartAction(ActionKind kind, int duration, int targetId = 0)
    {
        Action = kind;
        ActionDuration = duration;
        ActionElapsed = 0;
        ActionTargetId = targetId;
        if (kind != ActionKind.Casting)
        {
            ActionSpellId = "";
            ActionAim = default;
            ActionItemId = 0;
        }
    }

    internal void ClearAction() => StartAction(ActionKind.Idle, 0);

    internal void SetMoveIntent(Cell cell)
    {
        Intent = IntentKind.MoveTo;
        IntentCell = cell;
        IntentTargetId = 0;
        IntentSpellId = "";
        IntentFromStaff = false;
        IntentItemId = 0;
    }

    internal void SetAttackIntent(int targetId)
    {
        Intent = IntentKind.Attack;
        IntentCell = default;
        IntentTargetId = targetId;
        IntentSpellId = "";
        IntentFromStaff = false;
        IntentItemId = 0;
    }

    internal void SetCastIntent(string spellId, int targetId, Cell cell, bool fromStaff = false, int itemId = 0)
    {
        Intent = IntentKind.Cast;
        IntentSpellId = spellId;
        IntentTargetId = targetId;
        IntentCell = cell;
        IntentFromStaff = fromStaff;
        IntentItemId = itemId;
    }

    internal void SetPickUpIntent(int groundItemId, Cell cell)
    {
        Intent = IntentKind.PickUp;
        IntentCell = cell;
        IntentTargetId = groundItemId;
        IntentSpellId = "";
        IntentFromStaff = false;
        IntentItemId = 0;
    }

    internal void ClearIntent()
    {
        Intent = IntentKind.None;
        IntentCell = default;
        IntentTargetId = 0;
        IntentSpellId = "";
        IntentFromStaff = false;
        IntentItemId = 0;
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
