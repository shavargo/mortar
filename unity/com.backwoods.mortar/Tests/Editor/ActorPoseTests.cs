using Mortar.Game;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class ActorPoseTests
    {
        private static readonly Vector3 From = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 To = new Vector3(1f, 0f, 0f);

        [Test]
        public void ProgressAddsAlphaAndClamps()
        {
            Assert.AreEqual(0.5f, ActorPose.Progress(3, 7, 0.5f), 1e-4f);
            Assert.AreEqual(1f, ActorPose.Progress(9, 7, 0.5f));
            Assert.AreEqual(1f, ActorPose.Progress(0, 0, 0.3f));
        }

        [Test]
        public void MovingLerpsWithABobAtTheMidpoint()
        {
            var body = ActorPose.Body(ActionKind.Moving, 0.5f, From, To, To);

            Assert.AreEqual(0.5f, body.x, 1e-4f);
            Assert.AreEqual(ActorPose.BobHeight, body.y, 1e-4f);
        }

        [Test]
        public void MovingEndsExactlyOnTheDestination()
        {
            var body = ActorPose.Body(ActionKind.Moving, 1f, From, To, To);

            Assert.AreEqual(To.x, body.x, 1e-4f);
            Assert.AreEqual(To.y, body.y, 1e-4f);
        }

        [Test]
        public void AttackingLungesTowardTheTargetAndReturns()
        {
            var target = new Vector3(1f, 1f, 0f);

            var mid = ActorPose.Body(ActionKind.Attacking, 0.5f, To, To, target);
            var end = ActorPose.Body(ActionKind.Attacking, 1f, To, To, target);

            Assert.AreEqual(To.y + ActorPose.LungeDistance, mid.y, 1e-4f);
            Assert.AreEqual(To.y, end.y, 1e-4f);
        }

        [Test]
        public void IdleStaysOnItsCell()
        {
            Assert.AreEqual(To, ActorPose.Body(ActionKind.Idle, 0.4f, From, To, To));
        }

        [TestCase(Direction.West, true)]
        [TestCase(Direction.SouthWest, true)]
        [TestCase(Direction.South, true)]
        [TestCase(Direction.East, false)]
        [TestCase(Direction.NorthEast, false)]
        [TestCase(Direction.NorthWest, false)]
        public void FacesLeftFollowsTheScreenDirection(Direction facing, bool expected)
        {
            Assert.AreEqual(expected, ActorPose.FacesLeft(facing));
        }

        [Test]
        public void ArrowForEastPointsDownRight()
        {
            Assert.AreEqual(Mathf.Atan2(-0.25f, 0.5f) * Mathf.Rad2Deg, ActorPose.ArrowDegrees(Direction.East), 1e-3f);
        }
    }
}
