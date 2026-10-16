using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class GrassTrampersTests
    {
        [TestCase("Prop smith", true)]
        [TestCase("Prop healer", true)]
        [TestCase("Prop sage", true)]
        [TestCase("Prop stairs-down", false)]
        [TestCase("Actor 1", false)]
        public void TownNpcPropsAreRecognisedByName(string name, bool expected)
        {
            Assert.AreEqual(expected, GrassTrampers.IsTownNpc(name));
        }
    }
}
