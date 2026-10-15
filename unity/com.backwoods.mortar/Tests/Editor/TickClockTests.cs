using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class TickClockTests
    {
        [Test]
        public void FiftyMillisecondsIsOneTick()
        {
            var clock = new TickClock();

            Assert.AreEqual(1, clock.Advance(0.05f));
            Assert.AreEqual(0f, clock.Alpha, 1e-4f);
        }

        [Test]
        public void PartialTicksAccumulate()
        {
            var clock = new TickClock();

            Assert.AreEqual(0, clock.Advance(0.025f));
            Assert.AreEqual(0.5f, clock.Alpha, 1e-4f);
            Assert.AreEqual(1, clock.Advance(0.025f));
        }

        [Test]
        public void LongHitchIsCappedAndAlphaStaysBelowOne()
        {
            var clock = new TickClock();

            Assert.AreEqual(TickClock.MaxTicksPerFrame, clock.Advance(10f));
            Assert.That(clock.Alpha, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }

        [Test]
        public void NegativeTimeIsIgnored()
        {
            var clock = new TickClock();

            Assert.AreEqual(0, clock.Advance(-1f));
            Assert.AreEqual(0f, clock.Alpha);
        }

        [Test]
        public void ResetClearsTheRemainder()
        {
            var clock = new TickClock();
            clock.Advance(0.03f);

            clock.Reset();

            Assert.AreEqual(0f, clock.Alpha);
        }
    }
}
