namespace Mortar.Game.Content;

public sealed class ActionTimings
{
    public ActionTimings(int moveTicks, int attackTicks, int hitFrame, int recoveryTicks)
    {
        MoveTicks = moveTicks;
        AttackTicks = attackTicks;
        HitFrame = hitFrame;
        RecoveryTicks = recoveryTicks;
    }

    public int MoveTicks { get; }

    public int AttackTicks { get; }

    public int HitFrame { get; }

    public int RecoveryTicks { get; }
}
