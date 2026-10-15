using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class MessageLogTests
    {
        [Test]
        public void KeepsOnlyTheNewestMessages()
        {
            var log = new MessageLog();
            for (var i = 0; i < MessageLog.Capacity + 2; i++)
            {
                log.Add($"m{i}", 0f);
            }

            var visible = log.Visible(0f);

            Assert.AreEqual(MessageLog.Capacity, visible.Count);
            Assert.AreEqual("m2", visible[0]);
            Assert.AreEqual($"m{MessageLog.Capacity + 1}", visible[visible.Count - 1]);
        }

        [Test]
        public void MessagesExpire()
        {
            var log = new MessageLog();
            log.Add("old", 0f);
            log.Add("new", 3f);

            CollectionAssert.AreEqual(new[] { "new" }, log.Visible(MessageLog.LifetimeSeconds + 0.5f));
        }

        [Test]
        public void ClearRemovesEverything()
        {
            var log = new MessageLog();
            log.Add("a", 0f);

            log.Clear();

            Assert.IsEmpty(log.Visible(0f));
        }
    }
}
