using System.Collections.Generic;
using Mortar.Game.Items;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class ItemTooltip
    {
        private readonly List<string> _lines = new List<string>();

        public ItemTooltip()
        {
            Element = new VisualElement { name = "tooltip", pickingMode = PickingMode.Ignore };
            Element.style.minHeight = 72f;
            Element.style.marginTop = 8f;
        }

        public VisualElement Element { get; }

        public IReadOnlyList<string> Lines => _lines;

        public void Show(Item? item)
        {
            Element.Clear();
            _lines.Clear();
            if (item == null)
            {
                return;
            }
            _lines.AddRange(ItemText.Lines(item));
            for (var i = 0; i < _lines.Count; i++)
            {
                var label = Ui.Text(_lines[i], i == 0 ? 15 : 13);
                if (i == 0)
                {
                    label.style.color = ItemText.NameColor(item);
                }
                Element.Add(label);
            }
        }
    }
}
