using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public static class Ui
    {
        public const string FontPath = "Mortar/DejaVuSansMono";

        public static readonly Color PanelColor = new Color(0.02f, 0.02f, 0.02f, 0.92f);
        public static readonly Color TitleBackground = new Color(0.02f, 0.02f, 0.02f, 1f);
        public static readonly Color BorderColor = new Color(0.62f, 0.62f, 0.6f);
        public static readonly Color TextColor = new Color(0.86f, 0.86f, 0.82f);
        public static readonly Color MagicColor = new Color(0.45f, 0.6f, 1f);
        public static readonly Color HeldColor = new Color(0.95f, 0.8f, 0.3f);
        public static readonly Color SlotColor = new Color(0.06f, 0.06f, 0.06f, 0.95f);
        public static readonly Color EmptyBarColor = new Color(0.3f, 0.3f, 0.3f);

        public static bool ApplyFont(VisualElement root)
        {
            var font = Resources.Load<Font>(FontPath);
            if (font == null)
            {
                Debug.LogWarning($"Mortar: interface font '{FontPath}' did not load; using the default font");
                return false;
            }
            root.style.unityFontDefinition = FontDefinition.FromFont(font);
            return true;
        }

        public static VisualElement Box(string name, string? title = null)
        {
            var box = new VisualElement { name = name };
            box.style.backgroundColor = PanelColor;
            Border(box, 1f, BorderColor);
            Padding(box, 10f);
            if (title != null)
            {
                var label = Text($"┤ {title} ├", 14);
                label.name = "title";
                label.style.backgroundColor = TitleBackground;
                Place(label, 12f, -10f, null, null);
                box.style.paddingTop = 16f;
                box.Add(label);
            }
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
            Flat(button);
            return button;
        }

        public static void Flat(Button button)
        {
            button.style.color = TextColor;
            button.style.backgroundColor = SlotColor;
            Border(button, 1f, BorderColor);
            Radius(button, 0f);
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
