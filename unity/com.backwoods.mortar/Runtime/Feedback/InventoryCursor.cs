using System;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Items;

namespace Mortar.Unity
{
    public sealed class InventoryCursor
    {
        public int HeldItemId { get; private set; }

        public bool IsHolding => HeldItemId != 0;

        public void Release() => HeldItemId = 0;

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
                return Take(id => new MoveItemCommand(id, cell));
            }
            HeldItemId = inventory.ItemAt(cell);
            return null;
        }

        public Command ClickBelt(int slot) =>
            IsHolding ? Take(id => new PutInBeltCommand(id, slot)) : new UseBeltCommand(slot);

        public Command? ClickEquipment(EquipmentSlot slot, Equipment equipment)
        {
            if (IsHolding)
            {
                return Take(id => new EquipCommand(id));
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
