using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Items;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class InventoryCursorTests
    {
        private GameSession _session = null!;
        private Belongings _gear = null!;
        private Item _knife = null!;
        private InventoryCursor _cursor = null!;

        [SetUp]
        public void SetUp()
        {
            _session = TestContent.NewSession();
            _gear = _session.World.Belongings;
            _knife = _gear.Equipment.Get(EquipmentSlot.Weapon)!;
            _cursor = new InventoryCursor();
            Run(new UnequipCommand(EquipmentSlot.Weapon));
        }

        private void Run(Command? command)
        {
            Assert.IsNotNull(command);
            _session.Enqueue(command!);
            _session.Tick();
        }

        private Cell KnifeCell => _gear.Inventory.Find(_knife.Id)!.Cell;

        [Test]
        public void ClickingABagItemHoldsIt()
        {
            Assert.IsNull(_cursor.ClickBag(KnifeCell, _gear.Inventory));

            Assert.AreEqual(_knife.Id, _cursor.HeldItemId);
        }

        [Test]
        public void ClickingAnotherCellMovesTheHeldItem()
        {
            _cursor.ClickBag(KnifeCell, _gear.Inventory);

            Run(_cursor.ClickBag(new Cell(5, 0), _gear.Inventory));

            Assert.AreEqual(new Cell(5, 0), KnifeCell);
            Assert.IsFalse(_cursor.IsHolding);
        }

        [Test]
        public void ClickingAnEquipmentSlotWhileHoldingEquips()
        {
            _cursor.ClickBag(KnifeCell, _gear.Inventory);

            Run(_cursor.ClickEquipment(EquipmentSlot.Weapon, _gear.Equipment));

            Assert.AreEqual(_knife.Id, _gear.Equipment.Get(EquipmentSlot.Weapon)!.Id);
        }

        [Test]
        public void ClickingFilledEquipmentUnequipsAndEmptyDoesNothing()
        {
            Assert.IsInstanceOf<UnequipCommand>(_cursor.ClickEquipment(EquipmentSlot.Body, _gear.Equipment));
            Assert.IsNull(_cursor.ClickEquipment(EquipmentSlot.Head, _gear.Equipment));
        }

        [Test]
        public void DroppingReleasesTheHeldItem()
        {
            Assert.IsNull(_cursor.Drop());
            _cursor.ClickBag(KnifeCell, _gear.Inventory);

            Run(_cursor.Drop());

            Assert.IsFalse(_cursor.IsHolding);
            Assert.IsNull(_gear.Inventory.Find(_knife.Id));
        }

        [Test]
        public void BeltClicksUseTheSlotWhenNothingIsHeld()
        {
            var command = (UseBeltCommand)_cursor.ClickBelt(3);

            Assert.AreEqual(3, command.Slot);
        }

        [Test]
        public void QuickUseEquipsBagItemsAndUnequipsWornOnes()
        {
            Assert.IsInstanceOf<EquipCommand>(InventoryCursor.QuickUse(_knife, _gear));

            var vest = _gear.Equipment.Get(EquipmentSlot.Body)!;
            Assert.IsInstanceOf<UnequipCommand>(InventoryCursor.QuickUse(vest, _gear));
        }
    }
}
