using Mortar.Game.Commands;
using Mortar.Game.Items;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class InventoryScreenTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private MortarGame _game = null!;
        private InventoryScreen _screen = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("inventory-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _game = _root.AddComponent<MortarGame>();
            _game.Begin(TestContent.NewSession());
            _game.Paused = true;
            var ui = UiRoot.Create(_root.transform, UiRoot.CreatePanelSettings(null));
            _screen = new InventoryScreen(ui, _game, _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
        }

        private Belongings Gear => _game.Session!.World.Belongings;

        [Test]
        public void OpeningShowsEquippedItemsAndGold()
        {
            _screen.Open();
            _screen.Refresh(Gear);

            Assert.IsTrue(_screen.IsOpen);
            Assert.AreEqual(2, _screen.Equipment.FilledCount);
            Assert.AreEqual(0, _screen.ItemElementCount);
            Assert.AreEqual("Gold: 0", _screen.GoldText);
        }

        [Test]
        public void UnequippedItemsAppearInTheBag()
        {
            _game.Session!.Enqueue(new UnequipCommand(EquipmentSlot.Weapon));
            _game.RunTick();
            _screen.Open();

            _screen.Refresh(Gear);

            Assert.AreEqual(1, _screen.ItemElementCount);
            Assert.AreEqual(1, _screen.Equipment.FilledCount);
        }

        [Test]
        public void ClosingReleasesTheHeldItem()
        {
            _game.Session!.Enqueue(new UnequipCommand(EquipmentSlot.Weapon));
            _game.RunTick();
            _screen.Open();
            _screen.Cursor.ClickBag(Gear.Inventory.Items[0].Cell, Gear.Inventory);

            _screen.Toggle();

            Assert.IsFalse(_screen.IsOpen);
            Assert.IsFalse(_screen.Cursor.IsHolding);
        }
    }
}
