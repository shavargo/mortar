using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class TextBarTests
    {
        [TestCase(0, 100, 0)]
        [TestCase(1, 100, 1)]
        [TestCase(50, 100, 7)]
        [TestCase(96, 129, 11)]
        [TestCase(100, 100, 14)]
        [TestCase(150, 100, 14)]
        [TestCase(-5, 100, 0)]
        [TestCase(5, 0, 0)]
        public void FilledCellsRoundUpSoAnyLifeShows(int current, int max, int expected)
        {
            Assert.AreEqual(expected, TextBar.Filled(current, max));
        }

        [Test]
        public void BarsShowBlocksAndNumbers()
        {
            var bar = new TextBar("life", "HP", Color.red);

            bar.Set(96, 129);

            Assert.AreEqual("96/129", bar.Text);
            Assert.AreEqual(new string('█', 11) + new string('░', 3), bar.BarText);
        }

        [Test]
        public void NegativeLifeShowsAsZero()
        {
            var bar = new TextBar("life", "HP", Color.red);

            bar.Set(-3, 50);

            Assert.AreEqual("0/50", bar.Text);
        }
    }
}
