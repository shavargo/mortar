using System.Collections.Generic;
using Mortar.Game.Simulation;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ActorAnimatorTests
    {
        private static ClipSet Full() => new ClipSet(new Dictionary<string, ActorClip>
        {
            { ClipNames.Idle, Clip(ClipNames.Idle, 4, loop: true) },
            { ClipNames.Walk, Clip(ClipNames.Walk, 8, loop: true) },
            { ClipNames.Attack, Clip(ClipNames.Attack, 6, hitFrame: 3) },
            { ClipNames.Hit, Clip(ClipNames.Hit, 3) },
            { ClipNames.Death, Clip(ClipNames.Death, 5) },
        });

        private static ActorClip Clip(string name, int frames, int hitFrame = 0, bool loop = false) =>
            new ActorClip(SpriteKeys.Clip("warrior", name), null, TestClipInfo.Make(frames, 8, hitFrame, loop));

        private static AnimMoment Moment(ActionKind action, int elapsed = 0, int duration = 0, float alpha = 0f, float now = 0f) =>
            new AnimMoment
            {
                Action = action, Elapsed = elapsed, Duration = duration, HitTick = 4, Alpha = alpha, Now = now,
                HitAt = float.NegativeInfinity, DiedAt = -1f, IsDead = false,
            };

        private static void AssertFrame(ClipFrame frame, string name, int index)
        {
            Assert.AreEqual(name, frame.Name);
            Assert.AreEqual(index, frame.Index);
        }

        [TestCase(0.25f, 2)]
        [TestCase(0.45f, 0)]
        [TestCase(0.75f, 3)]
        public void IdleLoopsOnRealTime(float now, int expected)
        {
            AssertFrame(ActorAnimator.Pick(Full(), Moment(ActionKind.Idle, now: now)), ClipNames.Idle, expected);
        }

        [Test]
        public void WalkFollowsTheMoveAcrossOneCell()
        {
            AssertFrame(ActorAnimator.Pick(Full(), Moment(ActionKind.Moving, 2, 8)), ClipNames.Walk, 2);
            AssertFrame(ActorAnimator.Pick(Full(), Moment(ActionKind.Moving, 7, 8, alpha: 0.99f)), ClipNames.Walk, 7);
        }

        [TestCase(8, 4)]
        [TestCase(12, 5)]
        [TestCase(6, 1)]
        [TestCase(20, 15)]
        public void AttackShowsTheHitFrameExactlyOnTheHitTick(int attackTicks, int hitTick)
        {
            Assert.AreEqual(3, ActorAnimator.AttackFrame(hitTick, attackTicks, hitTick, 6, 3));
            Assert.Less(ActorAnimator.AttackFrame(hitTick - 0.01f, attackTicks, hitTick, 6, 3), 3);
            Assert.AreEqual(0, ActorAnimator.AttackFrame(0f, attackTicks, hitTick, 6, 3));
            Assert.AreEqual(5, ActorAnimator.AttackFrame(attackTicks, attackTicks, hitTick, 6, 3));
        }

        [Test]
        public void PickUsesTheAttackTimingOfTheActor()
        {
            var moment = Moment(ActionKind.Attacking, 4, 10);

            AssertFrame(ActorAnimator.Pick(Full(), moment), ClipNames.Attack, 3);
        }

        [Test]
        public void StaggerPlaysTheHitClipOverItsDuration()
        {
            AssertFrame(ActorAnimator.Pick(Full(), Moment(ActionKind.Recovering, 3, 6)), ClipNames.Hit, 1);
        }

        [Test]
        public void TheHitClipNeverJumpsBackWhenTheStaggerEnds()
        {
            var clips = new ClipSet(new Dictionary<string, ActorClip>
            {
                { ClipNames.Idle, Clip(ClipNames.Idle, 4, loop: true) },
                { ClipNames.Hit, Clip(ClipNames.Hit, 6) },
            });
            var staggering = Moment(ActionKind.Recovering, 5, 6, alpha: 0.99f, now: 10.299f);
            staggering.HitAt = 10f;
            var recovered = Moment(ActionKind.Idle, now: 10.3f);
            recovered.HitAt = 10f;

            var before = ActorAnimator.Pick(clips, staggering);
            var after = ActorAnimator.Pick(clips, recovered);

            Assert.AreEqual(ClipNames.Hit, before.Name);
            Assert.AreEqual(ClipNames.Hit, after.Name);
            Assert.GreaterOrEqual(after.Index, before.Index);
        }

        [Test]
        public void StaggerWithoutAHitClipShowsIdle()
        {
            var idleOnly = new ClipSet(new Dictionary<string, ActorClip> { { ClipNames.Idle, Clip(ClipNames.Idle, 4, loop: true) } });

            Assert.AreEqual(ClipNames.Idle, ActorAnimator.Pick(idleOnly, Moment(ActionKind.Recovering, 3, 6)).Name);
        }

        [TestCase(0)]
        [TestCase(8)]
        public void AttackWithoutAUsableHitTickFollowsProgress(int hitTick)
        {
            Assert.AreEqual(1, ActorAnimator.AttackFrame(2f, 8, hitTick, 6, 3));
        }

        [TestCase(1)]
        [TestCase(0)]
        public void AttackWithASingleFrameOrNoneShowsFrameZero(int frames)
        {
            Assert.AreEqual(0, ActorAnimator.AttackFrame(5f, 8, 4, frames, 0));
        }

        [Test]
        public void ARecentHitOverridesWalkingButNotAttacking()
        {
            var walking = Moment(ActionKind.Moving, 2, 8, now: 10.15f);
            walking.HitAt = 10f;
            var attacking = Moment(ActionKind.Attacking, 1, 10, now: 10.15f);
            attacking.HitAt = 10f;

            AssertFrame(ActorAnimator.Pick(Full(), walking), ClipNames.Hit, 1);
            Assert.AreEqual(ClipNames.Attack, ActorAnimator.Pick(Full(), attacking).Name);
        }

        [Test]
        public void AnOldHitNoLongerShows()
        {
            var moment = Moment(ActionKind.Idle, now: 10.5f);
            moment.HitAt = 10f;

            Assert.AreEqual(ClipNames.Idle, ActorAnimator.Pick(Full(), moment).Name);
        }

        [Test]
        public void WithoutAHitClipAHitKeepsTheCurrentClip()
        {
            var idleOnly = new ClipSet(new Dictionary<string, ActorClip> { { ClipNames.Idle, Clip(ClipNames.Idle, 4, loop: true) } });
            var moment = Moment(ActionKind.Idle, now: 10.1f);
            moment.HitAt = 10f;

            Assert.AreEqual(ClipNames.Idle, ActorAnimator.Pick(idleOnly, moment).Name);
        }

        [TestCase(0.25f, 2)]
        [TestCase(10f, 4)]
        public void DeathPlaysOnceAndHoldsTheLastFrame(float sinceDeath, int expected)
        {
            var moment = Moment(ActionKind.Attacking, 2, 10, now: 5f + sinceDeath);
            moment.IsDead = true;
            moment.DiedAt = 5f;

            AssertFrame(ActorAnimator.Pick(Full(), moment), ClipNames.Death, expected);
        }

        [Test]
        public void AMissingClipUsesIdleFrames()
        {
            var idleOnly = new ClipSet(new Dictionary<string, ActorClip> { { ClipNames.Idle, Clip(ClipNames.Idle, 4, loop: true) } });

            var frame = ActorAnimator.Pick(idleOnly, Moment(ActionKind.Moving, 7, 8));

            Assert.AreEqual(ClipNames.Walk, frame.Name);
            Assert.AreEqual(3, frame.Index);
        }
    }
}
