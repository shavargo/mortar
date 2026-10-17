using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class MenuScreen
    {
        private readonly VisualElement _box;
        private readonly Label _message;

        public MenuScreen(string title)
        {
            Element = new VisualElement { name = $"menu-{title.ToLowerInvariant()}" };
            Ui.Fill(Element);
            Element.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            Element.style.alignItems = Align.Center;
            Element.style.justifyContent = Justify.Center;
            _box = Ui.Box("menu-box");
            _box.style.alignItems = Align.Center;
            Ui.Padding(_box, 24f);
            _box.Add(Ui.Text(title, 28));
            _message = Ui.Text("", 14);
            _message.style.marginTop = 8f;
            _message.style.marginBottom = 8f;
            _message.style.whiteSpace = WhiteSpace.Normal;
            _message.style.maxWidth = 420f;
            _box.Add(_message);
            Element.Add(_box);
            SetMessage(null);
        }

        public VisualElement Element { get; }

        public string Message => _message.text;

        public Button AddButton(string text, Action onClick)
        {
            var button = Ui.MenuButton(text, onClick);
            _box.Add(button);
            return button;
        }

        public void SetMessage(string? message)
        {
            _message.text = message ?? "";
            Ui.Show(_message, message != null);
        }
    }
}
