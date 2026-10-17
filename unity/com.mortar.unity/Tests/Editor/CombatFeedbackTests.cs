using Mortar.Game.Events;
using Mortar.Game.Simulation;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class CombatFeedbackTests
    {
        [Test]
        public void HitsOnMonstersShowDamage()
        {
            var popup = CombatFeedback.For(new AttackResolved(1, Actor.PlayerId, 7, AttackOutcome.Hit, 12))!.Value;

            Assert.AreEqual(7, popup.ActorId);
            Assert.AreEqual("12", popup.Text);
            Assert.IsFalse(popup.HurtsPlayer);
            Assert.IsTrue(popup.IsHit);
        }

        [Test]
        public void HitsOnThePlayerAreMarked()
        {
            var popup = CombatFeedback.For(new AttackResolved(1, 7, Actor.PlayerId, AttackOutcome.Hit, 3))!.Value;

            Assert.IsTrue(popup.HurtsPlayer);
        }

        [Test]
        public void MissesAndBlocksAreNamed()
        {
            Assert.AreEqual("miss", CombatFeedback.For(new AttackResolved(1, 1, 7, AttackOutcome.Miss, 0))!.Value.Text);
            Assert.AreEqual("block", CombatFeedback.For(new AttackResolved(1, 1, 7, AttackOutcome.Blocked, 0))!.Value.Text);
            Assert.IsFalse(CombatFeedback.For(new AttackResolved(1, 1, 7, AttackOutcome.Miss, 0))!.Value.IsHit);
        }

        [Test]
        public void OtherEventsHaveNoPopup() =>
            Assert.IsNull(CombatFeedback.For(new MonsterAwoke(1, 7)));
    }
}
