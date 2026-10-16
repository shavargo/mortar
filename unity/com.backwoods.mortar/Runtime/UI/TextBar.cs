using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class TextBar
    {
        public const int Cells = 14;
        public const int FontSize = 15;

        private readonly Label _filled;
        private readonly Label _empty;
        private readonly Label _numbers;
        private int _current = int.MinValue;
        private int _max = int.MinValue;

        public TextBar(string name, string caption, Color fill)
        {
            Element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            Element.style.flexDirection = FlexDirection.Row;
            Element.style.backgroundColor = Ui.PanelColor;
            Ui.Border(Element, 1f, Ui.BorderColor);
            Ui.Padding(Element, 4f);
            Element.Add(Ui.Text(caption + " ", FontSize));
            _filled = Tight(Ui.Text("", FontSize), fill);
            _empty = Tight(Ui.Text("", FontSize), Ui.EmptyBarColor);
            _numbers = Ui.Text("", FontSize);
            _numbers.style.marginLeft = 8f;
            Element.Add(_filled);
            Element.Add(_empty);
            Element.Add(_numbers);
        }

        public VisualElement Element { get; }

        public string Text => _numbers.text;

        public string BarText => _filled.text + _empty.text;

        public void Set(int current, int max)
        {
            var shown = Mathf.Max(0, current);
            if (shown == _current && max == _max)
            {
                return;
            }
            _current = shown;
            _max = max;
            var filled = Filled(shown, max);
            _filled.text = new string('█', filled);
            _empty.text = new string('░', Cells - filled);
            _numbers.text = $"{shown}/{max}";
        }

        public static int Filled(int current, int max) =>
            max <= 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt((float)current * Cells / max), 0, Cells);

        private static Label Tight(Label label, Color color)
        {
            label.style.color = color;
            label.style.marginLeft = 0f;
            label.style.marginRight = 0f;
            label.style.paddingLeft = 0f;
            label.style.paddingRight = 0f;
            return label;
        }
    }
}
