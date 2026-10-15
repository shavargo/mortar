using System.Collections.Generic;
using System.Text;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Items;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class InventoryScreen
    {
        public const float CellSize = 36f;

        private readonly MortarGame _game;
        private readonly SpriteCatalog _catalog;
        private readonly VisualElement _panel;
        private readonly VisualElement _grid;
        private readonly Label _gold;
        private readonly ItemTooltip _tooltip = new ItemTooltip();
        private readonly List<VisualElement> _itemElements = new List<VisualElement>();
        private string _shownKey = "";

        public InventoryScreen(UiRoot ui, MortarGame game, SpriteCatalog catalog)
        {
            _game = game;
            _catalog = catalog;
            Equipment = new EquipmentPanel(catalog);
            Equipment.SlotClicked += OnEquipmentClicked;
            Equipment.SlotHovered += slot => _tooltip.Show(slot.HasValue ? Gear?.Equipment.Get(slot.Value) : null);
            _gold = Ui.Text("", 14);
            _grid = BuildGrid();
            _panel = Ui.Box("inventory");
            Ui.Place(_panel, null, 16f, 16f, null);
            _panel.Add(Equipment.Element);
            _panel.Add(_gold);
            _panel.Add(_grid);
            _panel.Add(_tooltip.Element);
            Ui.Show(_panel, false);
            ui.Screens.Add(_panel);
        }

        public InventoryCursor Cursor { get; } = new InventoryCursor();

        public EquipmentPanel Equipment { get; }

        public bool IsOpen => Ui.IsShown(_panel);

        public int ItemElementCount => _itemElements.Count;

        public string GoldText => _gold.text;

        private Belongings? Gear => _game.Session?.World.Belongings;

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            _shownKey = "";
            Ui.Show(_panel, true);
        }

        public void Close()
        {
            Cursor.Release();
            _tooltip.Show(null);
            Ui.Show(_panel, false);
        }

        public bool DropHeld()
        {
            var command = Cursor.Drop();
            if (command == null)
            {
                return false;
            }
            _game.Enqueue(command);
            return true;
        }

        public void Refresh(Belongings belongings)
        {
            if (!IsOpen)
            {
                return;
            }
            Cursor.Validate(belongings.Inventory);
            var key = SnapshotKey(belongings);
            if (key == _shownKey)
            {
                return;
            }
            _shownKey = key;
            Equipment.Refresh(belongings.Equipment);
            _gold.text = $"Gold: {belongings.Gold}";
            RebuildItems(belongings.Inventory);
        }

        private string SnapshotKey(Belongings belongings)
        {
            var key = new StringBuilder();
            key.Append(belongings.Gold).Append('|').Append(Cursor.HeldItemId);
            foreach (var stored in belongings.Inventory.Items)
            {
                key.Append('|').Append(stored.Item.Id).Append('@').Append(stored.Cell.X).Append(',').Append(stored.Cell.Y);
            }
            foreach (var worn in belongings.Equipment.Worn)
            {
                key.Append("|w").Append(worn.Id);
            }
            return key.ToString();
        }

        private void RebuildItems(Inventory inventory)
        {
            foreach (var element in _itemElements)
            {
                element.RemoveFromHierarchy();
            }
            _itemElements.Clear();
            foreach (var stored in inventory.Items)
            {
                var element = ItemElement(stored);
                _grid.Add(element);
                _itemElements.Add(element);
            }
        }

        private VisualElement ItemElement(StoredItem stored)
        {
            var definition = stored.Item.Definition;
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            Ui.Place(element, stored.Cell.X * CellSize, stored.Cell.Y * CellSize, null, null);
            element.style.width = definition.Width * CellSize;
            element.style.height = definition.Height * CellSize;
            element.style.alignItems = Align.Center;
            element.style.justifyContent = Justify.Center;
            element.style.backgroundColor = new Color(0.22f, 0.19f, 0.15f, 0.95f);
            Ui.Border(element, 1f, stored.Item.Id == Cursor.HeldItemId ? Ui.HeldColor : Ui.BorderColor);
            element.Add(Ui.Icon(_catalog.Icon(definition.IconId), Mathf.Min(definition.Width, definition.Height) * CellSize - 4f));
            return element;
        }

        private VisualElement BuildGrid()
        {
            var grid = new VisualElement { name = "bag" };
            grid.style.width = Inventory.Width * CellSize;
            grid.style.height = Inventory.Height * CellSize;
            grid.style.marginTop = 8f;
            grid.style.backgroundColor = Ui.SlotColor;
            Ui.Border(grid, 1f, Ui.BorderColor);
            grid.RegisterCallback<PointerDownEvent>(OnGridClicked);
            grid.RegisterCallback<PointerMoveEvent>(evt => _tooltip.Show(ItemAt(evt.localPosition)));
            grid.RegisterCallback<PointerLeaveEvent>(evt => _tooltip.Show(null));
            return grid;
        }

        private void OnGridClicked(PointerDownEvent evt)
        {
            evt.StopPropagation();
            var gear = Gear;
            if (gear == null)
            {
                return;
            }
            if (evt.button == 1)
            {
                var item = ItemAt(evt.localPosition);
                Send(item != null ? InventoryCursor.QuickUse(item, gear) : null);
                return;
            }
            Send(Cursor.ClickBag(CellAt(evt.localPosition), gear.Inventory));
        }

        private void OnEquipmentClicked(EquipmentSlot slot, int button)
        {
            var gear = Gear;
            if (gear == null)
            {
                return;
            }
            if (button == 1)
            {
                Send(gear.Equipment.Get(slot) != null ? new UnequipCommand(slot) : null);
                return;
            }
            Send(Cursor.ClickEquipment(slot, gear.Equipment));
        }

        private void Send(Command? command)
        {
            if (command != null)
            {
                _game.Enqueue(command);
            }
        }

        private Item? ItemAt(Vector3 local)
        {
            var gear = Gear;
            if (gear == null)
            {
                return null;
            }
            var id = gear.Inventory.ItemAt(CellAt(local));
            return id == 0 ? null : gear.Inventory.Find(id)?.Item;
        }

        private static Cell CellAt(Vector3 local) =>
            new Cell(Mathf.FloorToInt(local.x / CellSize), Mathf.FloorToInt(local.y / CellSize));
    }
}
