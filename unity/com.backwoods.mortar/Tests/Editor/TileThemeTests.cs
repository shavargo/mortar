using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class TileThemeTests
    {
        [TestCase("cathedral-3", "cathedral")]
        [TestCase("crypt-12", "crypt")]
        [TestCase("town", "town")]
        [TestCase("old-town", "old-town")]
        [TestCase("level-", "level-")]
        [TestCase("-4", "-4")]
        public void StripsATrailingNumber(string areaId, string expected)
        {
            Assert.AreEqual(expected, TileTheme.For(areaId));
        }
    }
}
