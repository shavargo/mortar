using System.Collections.Generic;
using Mortar.Game.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class HudView
    {
        public const float ExperienceWidth = 360f;

        private readonly VisualElement _bar;
        private readonly VisualElement _experienceFill;
        private readonly VisualElement _messages;
        private readonly Label _hover;
        private readonly List<string> _shownMessages = new List<string>();

        public HudView(UiRoot ui, SpriteCatalog catalog)
        {
            Life = new Orb("life", new Color(0.65f, 0.08f, 0.08f));
            Mana = new Orb("mana", new Color(0.12f, 0.2f, 0.7f));
            Belt = new BeltBar(catalog);
            _experienceFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _bar = BuildBottomBar(BuildCenter());
            ui.Hud.Add(_bar);
            _messages = new VisualElement { name = "messages", pickingMode = PickingMode.Ignore };
            Ui.Place(_messages, 0f, 12f, 0f, null);
            _messages.style.alignItems = Align.Center;
            ui.Hud.Add(_messages);
            _hover = Ui.Text("", 15);
            Ui.Place(_hover, 0f, 110f, 0f, null);
            _hover.style.unityTextAlign = TextAnchor.MiddleCenter;
            ui.Hud.Add(_hover);
        }

        public Orb Life { get; }

        public Orb Mana { get; }

        public BeltBar Belt { get; }

        public IReadOnlyList<string> MessageTexts => _shownMessages;

        public string HoverTextShown => _hover.text;

        public void Refresh(World world, IReadOnlyList<string> messages, string? hover)
        {
            var player = world.Player;
            Life.Set(player.Life, player.Profile.MaxLife);
            Mana.Set(player.Mana, player.Profile.MaxMana);
            Belt.Refresh(world.Belongings.Belt);
            _experienceFill.style.width = Length.Percent(ExperienceBar.Fraction(world.Progress, world.Content.Rules) * 100f);
            ShowMessages(messages);
            _hover.text = hover ?? "";
        }

        public void Show(bool visible)
        {
            Ui.Show(_bar, visible);
            Ui.Show(_messages, visible);
            Ui.Show(_hover, visible);
        }

        private void ShowMessages(IReadOnlyList<string> messages)
        {
            if (SameMessages(messages))
            {
                return;
            }
            _shownMessages.Clear();
            _shownMessages.AddRange(messages);
            _messages.Clear();
            foreach (var message in messages)
            {
                _messages.Add(Ui.Text(message, 15));
            }
        }

        private bool SameMessages(IReadOnlyList<string> messages)
        {
            if (messages.Count != _shownMessages.Count)
            {
                return false;
            }
            for (var i = 0; i < messages.Count; i++)
            {
                if (messages[i] != _shownMessages[i]) return false;
            }
            return true;
        }

        private VisualElement BuildCenter()
        {
            var center = new VisualElement { pickingMode = PickingMode.Ignore };
            center.style.alignItems = Align.Center;
            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.style.width = ExperienceWidth;
            track.style.height = 6f;
            track.style.marginBottom = 6f;
            track.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            _experienceFill.style.height = Length.Percent(100f);
            _experienceFill.style.backgroundColor = new Color(0.8f, 0.7f, 0.3f);
            track.Add(_experienceFill);
            center.Add(track);
            center.Add(Belt.Element);
            return center;
        }

        private VisualElement BuildBottomBar(VisualElement center)
        {
            var bar = new VisualElement { name = "hud-bar", pickingMode = PickingMode.Ignore };
            Ui.Place(bar, 16f, null, 16f, 8f);
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.justifyContent = Justify.SpaceBetween;
            bar.style.alignItems = Align.FlexEnd;
            bar.Add(Life.Element);
            bar.Add(center);
            bar.Add(Mana.Element);
            return bar;
        }
    }
}
