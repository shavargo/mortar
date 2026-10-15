using System;
using Mortar.Game.Items;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class BeltBar
    {
        public const float SlotSize = 44f;

        private readonly SpriteCatalog _catalog;
        private readonly Image[] _icons = new Image[Belt.Slots];

        public BeltBar(SpriteCatalog catalog)
        {
            _catalog = catalog;
            Element = new VisualElement { name = "belt", pickingMode = PickingMode.Ignore };
            Element.style.flexDirection = FlexDirection.Row;
            for (var i = 0; i < Belt.Slots; i++)
            {
                Element.Add(CreateSlot(i));
            }
        }

        public event Action<int>? SlotClicked;

        public VisualElement Element { get; }

        public int FilledSlots
        {
            get
            {
                var filled = 0;
                foreach (var icon in _icons)
                {
                    if (icon.sprite != null) filled++;
                }
                return filled;
            }
        }

        public void Refresh(Belt belt)
        {
            for (var i = 0; i < Belt.Slots; i++)
            {
                var item = belt.Get(i);
                _icons[i].sprite = item != null ? _catalog.Icon(item.Definition.IconId) : null;
            }
        }

        private VisualElement CreateSlot(int index)
        {
            var slot = new VisualElement { name = $"belt-{index + 1}" };
            slot.style.width = SlotSize;
            slot.style.height = SlotSize;
            slot.style.marginLeft = 2f;
            slot.style.marginRight = 2f;
            slot.style.backgroundColor = Ui.SlotColor;
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;
            Ui.Border(slot, 1f, Ui.BorderColor);
            _icons[index] = Ui.Icon(null, SlotSize - 8f);
            slot.Add(_icons[index]);
            var key = Ui.Text((index + 1).ToString(), 10);
            Ui.Place(key, 2f, 0f, null, null);
            slot.Add(key);
            slot.RegisterCallback<PointerDownEvent>(evt => SlotClicked?.Invoke(index));
            return slot;
        }
    }
}
