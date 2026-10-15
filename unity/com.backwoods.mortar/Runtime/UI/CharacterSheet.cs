using System.Collections.Generic;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Simulation;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class CharacterSheet
    {
        private readonly MortarGame _game;
        private readonly VisualElement _panel;
        private readonly Label _header = Ui.Text("", 18);
        private readonly Label _experience = Ui.Text("");
        private readonly Label _points = Ui.Text("");
        private readonly Label _vitals = Ui.Text("");
        private readonly Label _combat = Ui.Text("");
        private readonly Dictionary<StatKind, Label> _stats = new Dictionary<StatKind, Label>();
        private readonly Dictionary<StatKind, Button> _plus = new Dictionary<StatKind, Button>();

        public CharacterSheet(UiRoot ui, MortarGame game)
        {
            _game = game;
            _panel = Ui.Box("character");
            Ui.Place(_panel, 16f, 16f, null, null);
            _panel.style.width = 300f;
            _panel.Add(_header);
            _panel.Add(_experience);
            _panel.Add(_points);
            foreach (var kind in Stats.Kinds)
            {
                _panel.Add(StatRow(kind));
            }
            _panel.Add(_vitals);
            _panel.Add(_combat);
            Ui.Show(_panel, false);
            ui.Screens.Add(_panel);
        }

        public bool IsOpen => Ui.IsShown(_panel);

        public bool CanSpend => Ui.IsShown(_plus[StatKind.Strength]);

        public string HeaderText => _header.text;

        public string StatText(StatKind kind) => _stats[kind].text;

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open() => Ui.Show(_panel, true);

        public void Close() => Ui.Show(_panel, false);

        public void Refresh(World world)
        {
            if (!IsOpen)
            {
                return;
            }
            var progress = world.Progress;
            var profile = world.Player.Profile;
            _header.text = $"{world.PlayerClass.Name}  -  Level {progress.Level}";
            var next = ExperienceBar.NextThreshold(progress, world.Content.Rules);
            _experience.text = next.HasValue ? $"Experience: {progress.Experience} / {next.Value}" : $"Experience: {progress.Experience} (max level)";
            _points.text = $"Unspent points: {progress.UnspentPoints}";
            Ui.Show(_points, progress.UnspentPoints > 0);
            foreach (var kind in Stats.Kinds)
            {
                _stats[kind].text = $"{kind}: {progress.BaseStats.Get(kind)}";
                Ui.Show(_plus[kind], progress.UnspentPoints > 0);
            }
            _vitals.text = $"Life: {world.Player.Life}/{profile.MaxLife}\nMana: {world.Player.Mana}/{profile.MaxMana}";
            _combat.text = $"Damage: {profile.Damage.Min}-{profile.Damage.Max}\nArmor class: {profile.ArmorClass}\nTo hit: {profile.ToHit}\nBlock: {profile.BlockChance}%";
        }

        private VisualElement StatRow(StatKind kind)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.minHeight = 44f;
            _stats[kind] = Ui.Text("", 15);
            row.Add(_stats[kind]);
            var plus = new Button(() => _game.Enqueue(new SpendStatPointCommand(kind))) { text = "+" };
            plus.style.minWidth = 44f;
            plus.style.minHeight = 44f;
            Ui.Show(plus, false);
            _plus[kind] = plus;
            row.Add(plus);
            return row;
        }
    }
}
