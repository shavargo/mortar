using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class Orb
    {
        public const float Size = 96f;

        private readonly VisualElement _fill;
        private readonly Label _label;

        public Orb(string name, Color color)
        {
            Element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            Element.style.width = Size;
            Element.style.height = Size;
            Element.style.overflow = Overflow.Hidden;
            Element.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            Ui.Radius(Element, Size / 2f);
            Ui.Border(Element, 3f, Ui.BorderColor);
            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            Ui.Place(_fill, 0f, null, 0f, 0f);
            _fill.style.backgroundColor = color;
            Element.Add(_fill);
            _label = Ui.Text("", 13);
            Ui.Place(_label, 0f, Size / 2f - 10f, 0f, null);
            _label.style.unityTextAlign = TextAnchor.MiddleCenter;
            Element.Add(_label);
        }

        public VisualElement Element { get; }

        public string Text => _label.text;

        public void Set(int current, int max)
        {
            var fraction = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            _fill.style.height = Length.Percent(fraction * 100f);
            _label.text = $"{Mathf.Max(0, current)}/{max}";
        }
    }
}
