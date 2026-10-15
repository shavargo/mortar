using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public static class Ui
    {
        public static readonly Color PanelColor = new Color(0.08f, 0.07f, 0.06f, 0.94f);
        public static readonly Color BorderColor = new Color(0.45f, 0.36f, 0.22f);
        public static readonly Color TextColor = new Color(0.9f, 0.86f, 0.76f);
        public static readonly Color MagicColor = new Color(0.45f, 0.6f, 1f);
        public static readonly Color HeldColor = new Color(0.95f, 0.8f, 0.3f);
        public static readonly Color SlotColor = new Color(0.14f, 0.12f, 0.1f, 0.95f);

        public static VisualElement Box(string name)
        {
            var box = new VisualElement { name = name };
            box.style.backgroundColor = PanelColor;
            Border(box, 2f, BorderColor);
            Padding(box, 10f);
            return box;
        }

        public static Label Text(string text, int size = 14)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.style.color = TextColor;
            label.style.fontSize = size;
            return label;
        }

        public static Button MenuButton(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.minHeight = 44f;
            button.style.minWidth = 220f;
            button.style.fontSize = 16;
            button.style.marginTop = 4f;
            button.style.marginBottom = 4f;
            return button;
        }

        public static Image Icon(Sprite? sprite, float size)
        {
            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            image.style.width = size;
            image.style.height = size;
            return image;
        }

        public static void Border(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }

        public static void Padding(VisualElement element, float amount)
        {
            element.style.paddingTop = amount;
            element.style.paddingBottom = amount;
            element.style.paddingLeft = amount;
            element.style.paddingRight = amount;
        }

        public static void Radius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        public static void Fill(VisualElement element) => Place(element, 0f, 0f, 0f, 0f);

        public static void Place(VisualElement element, float? left, float? top, float? right, float? bottom)
        {
            element.style.position = Position.Absolute;
            if (left.HasValue) element.style.left = left.Value;
            if (top.HasValue) element.style.top = top.Value;
            if (right.HasValue) element.style.right = right.Value;
            if (bottom.HasValue) element.style.bottom = bottom.Value;
        }

        public static void Show(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        public static bool IsShown(VisualElement element) => element.style.display.value != DisplayStyle.None;
    }
}
