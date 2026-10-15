using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class EventTextTests
    {
        [Test]
        public void RejectionsReadAsSentences() =>
            Assert.AreEqual("There is nothing above", EventText.Describe(new CommandRejected(1, new UseStairsCommand(), "there is nothing above")));

        [Test]
        public void FailedPickupsGiveTheReason() =>
            Assert.AreEqual("Cannot pick that up: no room", EventText.Describe(new PickupFailed(1, 4, "no room")));

        [Test]
        public void PickupsNameTheItem() =>
            Assert.AreEqual("Picked up Iron Knife", EventText.Describe(new ItemPickedUp(1, 4, "Iron Knife")));

        [Test]
        public void GoldPickupsGiveTheAmount() =>
            Assert.AreEqual("Picked up 25 gold", EventText.Describe(new GoldPickedUp(1, 25, 100)));

        [Test]
        public void PotionsSayWhatTheyRestored() =>
            Assert.AreEqual("Restored 30 life", EventText.Describe(new PotionDrunk(1, 3, "life", 30)));

        [Test]
        public void LevelUpsGiveTheNewLevel() =>
            Assert.AreEqual("Reached level 2", EventText.Describe(new LeveledUp(1, 2)));

        [Test]
        public void StatIncreasesNameTheStat() =>
            Assert.AreEqual("Strength is now 25", EventText.Describe(new StatIncreased(1, StatKind.Strength, 25)));

        [Test]
        public void LevelChangesNameTheDestination()
        {
            Assert.AreEqual("Entered depth 1", EventText.Describe(new LevelChanged(1, 0, 1, new Cell(2, 2))));
            Assert.AreEqual("Entered the town", EventText.Describe(new LevelChanged(1, 1, 0, new Cell(2, 2))));
        }

        [Test]
        public void OtherEventsAreSilent() =>
            Assert.IsNull(EventText.Describe(new MonsterAwoke(1, 5)));
    }
}
