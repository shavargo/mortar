using Mortar.Game.Items;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ItemTextTests
    {
        private static Belongings StartingGear() => TestContent.NewSession().World.Belongings;

        [Test]
        public void WeaponsShowTheirDamage()
        {
            var knife = StartingGear().Equipment.Get(EquipmentSlot.Weapon)!;

            CollectionAssert.AreEqual(new[] { "Iron Knife", "Damage: 2-5" }, ItemText.Lines(knife));
        }

        [Test]
        public void ArmorShowsItsRolledValue()
        {
            var vest = StartingGear().Equipment.Get(EquipmentSlot.Body)!;

            Assert.AreEqual($"Armor: {vest.ArmorValue}", ItemText.Lines(vest)[1]);
            Assert.That(vest.ArmorValue, Is.InRange(3, 6));
        }

        [Test]
        public void PotionsShowWhatTheyRestore()
        {
            var potion = StartingGear().Belt.Get(0)!;

            CollectionAssert.AreEqual(new[] { "Healing Draught", "Restores 30 life" }, ItemText.Lines(potion));
        }

        [Test]
        public void BonusNamesAreReadable()
        {
            Assert.AreEqual("armor class", ItemText.BonusName("armorClass"));
            Assert.AreEqual("to hit", ItemText.BonusName("toHit"));
            Assert.AreEqual("strength", ItemText.BonusName("strength"));
        }
    }
}
