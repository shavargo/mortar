using Mortar.Game.Content;
using Mortar.Game.Simulation;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ExperienceBarTests
    {
        private static GameRules Rules => TestContent.Content.Content.Rules;

        private static PlayerProgress Progress(int level, long experience) =>
            new PlayerProgress("warrior", level, experience, new Stats(10, 10, 10, 10), 0);

        [Test]
        public void FirstLevelFillsTowardTheFirstThreshold()
        {
            var threshold = Rules.ExperienceThresholds[0];

            Assert.AreEqual(0.5f, ExperienceBar.Fraction(Progress(1, threshold / 2), Rules), 1e-4f);
            Assert.AreEqual(threshold, ExperienceBar.NextThreshold(Progress(1, 0), Rules));
        }

        [Test]
        public void LaterLevelsFillBetweenThresholds()
        {
            var low = Rules.ExperienceThresholds[0];
            var high = Rules.ExperienceThresholds[1];

            Assert.AreEqual(0.5f, ExperienceBar.Fraction(Progress(2, (low + high) / 2), Rules), 1e-4f);
        }

        [Test]
        public void MaxLevelIsFull()
        {
            Assert.AreEqual(1f, ExperienceBar.Fraction(Progress(Rules.MaxLevel, 0), Rules));
            Assert.IsNull(ExperienceBar.NextThreshold(Progress(Rules.MaxLevel, 0), Rules));
        }
    }
}
