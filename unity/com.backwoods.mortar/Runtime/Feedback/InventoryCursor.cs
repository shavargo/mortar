using System;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Items;

namespace Mortar.Unity
{
    public sealed class InventoryCursor
    {
        private Item? _held;
        private Cell _grabOffset;

        public int HeldItemId => _held != null ? _held.Id : 0;

        public bool IsHolding => _held != null;

        public void Release() => _held = null;

        public void Validate(Inventory inventory)
        {
            if (IsHolding && inventory.Find(HeldItemId) == null)
            {
                Release();
            }
        }

        public Command? ClickBag(Cell cell, Inventory inventory)
        {
            if (IsHolding)
            {
                var target = new Cell(cell.X - _grabOffset.X, cell.Y - _grabOffset.Y);
                return Take(id => new MoveItemCommand(id, target));
            }
            var stored = inventory.Find(inventory.ItemAt(cell));
            _held = stored != null ? stored.Item : null;
            _grabOffset = stored != null ? new Cell(cell.X - stored.Cell.X, cell.Y - stored.Cell.Y) : new Cell(0, 0);
            return null;
        }

        public Command ClickBelt(int slot) =>
            IsHolding ? Take(id => new PutInBeltCommand(id, slot)) : new UseBeltCommand(slot);

        public Command? ClickEquipment(EquipmentSlot slot, Equipment equipment)
        {
            if (_held != null)
            {
                return Equipment.Accepts(slot, _held.Definition) ? Take(id => new EquipCommand(id)) : null;
            }
            return equipment.Get(slot) != null ? new UnequipCommand(slot) : null;
        }

        public Command? Drop() => IsHolding ? Take(id => new DropItemCommand(id)) : null;

        public static Command? QuickUse(Item item, Belongings belongings)
        {
            var worn = belongings.Equipment.SlotOf(item.Id);
            if (worn.HasValue)
            {
                return new UnequipCommand(worn.Value);
            }
            if (item.Definition.IsPotion)
            {
                var slot = belongings.Belt.FirstEmpty();
                return slot >= 0 ? new PutInBeltCommand(item.Id, slot) : null;
            }
            return item.Definition.IsEquippable ? new EquipCommand(item.Id) : null;
        }

        private Command Take(Func<int, Command> create)
        {
            var command = create(HeldItemId);
            Release();
            return command;
        }
    }
}
