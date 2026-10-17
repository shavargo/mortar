using System;
using System.Collections.Generic;
using Mortar.Game.Items;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class EquipmentPanel
    {
        public const float SlotSize = 64f;

        private readonly SpriteCatalog _catalog;
        private readonly Dictionary<EquipmentSlot, Image> _icons = new Dictionary<EquipmentSlot, Image>();

        public EquipmentPanel(SpriteCatalog catalog)
        {
            _catalog = catalog;
            Element = new VisualElement { name = "equipment" };
            Element.style.flexDirection = FlexDirection.Row;
            Element.style.flexWrap = Wrap.Wrap;
            Element.style.justifyContent = Justify.Center;
            foreach (var slot in Equipment.Slots)
            {
                Element.Add(CreateSlot(slot));
            }
        }

        public event Action<EquipmentSlot, int>? SlotClicked;

        public event Action<EquipmentSlot?>? SlotHovered;

        public VisualElement Element { get; }

        public int FilledCount { get; private set; }

        public void Refresh(Equipment equipment)
        {
            FilledCount = 0;
            foreach (var slot in Equipment.Slots)
            {
                var item = equipment.Get(slot);
                _icons[slot].sprite = item != null ? _catalog.Icon(item.Definition.IconId) : null;
                if (item != null) FilledCount++;
            }
        }

        private VisualElement CreateSlot(EquipmentSlot slot)
        {
            var element = new VisualElement { name = $"equip-{slot}" };
            element.style.width = SlotSize;
            element.style.height = SlotSize;
            element.style.marginLeft = 3f;
            element.style.marginRight = 3f;
            element.style.marginTop = 3f;
            element.style.marginBottom = 3f;
            element.style.backgroundColor = Ui.SlotColor;
            element.style.alignItems = Align.Center;
            element.style.justifyContent = Justify.Center;
            Ui.Border(element, 1f, Ui.BorderColor);
            _icons[slot] = Ui.Icon(null, SlotSize - 12f);
            element.Add(_icons[slot]);
            var label = Ui.Text(slot.ToString(), 9);
            Ui.Place(label, 3f, 1f, null, null);
            element.Add(label);
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                SlotClicked?.Invoke(slot, evt.button);
                evt.StopPropagation();
            });
            element.RegisterCallback<PointerEnterEvent>(evt => SlotHovered?.Invoke(slot));
            element.RegisterCallback<PointerLeaveEvent>(evt => SlotHovered?.Invoke(null));
            return element;
        }
    }
}
