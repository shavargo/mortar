using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class GlyphRulesTests
    {
        [TestCase(0f, 0)]
        [TestCase(0.059f, 0)]
        [TestCase(0.06f, 1)]
        [TestCase(0.5f, 6)]
        [TestCase(0.99f, 11)]
        [TestCase(1f, 11)]
        public void RampSlotsFollowBrightnessAboveTheDarkThreshold(float luminance, int expected)
        {
            Assert.AreEqual(expected, GlyphRules.RampSlot(luminance, 0.06f));
        }

        [TestCase(-0.5f, 0f, 14)]
        [TestCase(0.5f, 0f, 14)]
        [TestCase(0f, -0.5f, 12)]
        [TestCase(-0.4f, -0.4f, 15)]
        [TestCase(0.4f, -0.4f, 13)]
        [TestCase(0.1f, 0.1f, 0)]
        public void EdgeSlotsRunAcrossTheGradient(float gx, float gy, int expected)
        {
            Assert.AreEqual(expected, GlyphRules.EdgeSlot(gx, gy, 0.25f));
        }
    }
}
