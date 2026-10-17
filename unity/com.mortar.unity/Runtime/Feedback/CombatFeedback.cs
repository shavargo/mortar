using System.Globalization;
using Mortar.Game.Events;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public readonly struct Popup
    {
        public Popup(int actorId, string text, bool hurtsPlayer, bool isHit)
        {
            ActorId = actorId;
            Text = text;
            HurtsPlayer = hurtsPlayer;
            IsHit = isHit;
        }

        public int ActorId { get; }

        public string Text { get; }

        public bool HurtsPlayer { get; }

        public bool IsHit { get; }
    }

    public static class CombatFeedback
    {
        public static Popup? For(GameEvent gameEvent)
        {
            if (!(gameEvent is AttackResolved attack))
            {
                return null;
            }
            var hurtsPlayer = attack.TargetId == Actor.PlayerId;
            switch (attack.Outcome)
            {
                case AttackOutcome.Hit:
                    return new Popup(attack.TargetId, attack.Damage.ToString(CultureInfo.InvariantCulture), hurtsPlayer, true);
                case AttackOutcome.Blocked:
                    return new Popup(attack.TargetId, "block", hurtsPlayer, false);
                default:
                    return new Popup(attack.TargetId, "miss", hurtsPlayer, false);
            }
        }
    }
}
