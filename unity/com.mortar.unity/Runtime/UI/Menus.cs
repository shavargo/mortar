using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class Menus
    {
        private readonly MenuScreen _main = new MenuScreen("Mortar");
        private readonly MenuScreen _pause = new MenuScreen("Paused");
        private readonly MenuScreen _death = new MenuScreen("You have died");
        private readonly Button _continue;

        public Menus(UiRoot ui, MortarApp app)
        {
            _main.AddButton("New Game", app.StartNewGame);
            _continue = _main.AddButton("Continue", app.ContinueGame);
            _main.AddButton("Quit", app.Quit);
            _pause.AddButton("Resume", app.TogglePause);
            _pause.AddButton("Save", app.SaveGame);
            _pause.AddButton("Load", app.ContinueGame);
            _pause.AddButton("Quit to Menu", app.QuitToMenu);
            _death.AddButton("Load Last Save", app.ContinueGame);
            _death.AddButton("New Game", app.StartNewGame);
            ui.Modal.Add(_main.Element);
            ui.Modal.Add(_pause.Element);
            ui.Modal.Add(_death.Element);
            HideAll();
        }

        public bool IsMainOpen => Ui.IsShown(_main.Element);

        public bool IsPauseOpen => Ui.IsShown(_pause.Element);

        public bool IsDeathOpen => Ui.IsShown(_death.Element);

        public bool AnyOpen => IsMainOpen || IsPauseOpen || IsDeathOpen;

        public string MainMessage => _main.Message;

        public string PauseMessage => _pause.Message;

        public void ShowMain(string? message, bool canContinue)
        {
            HideAll();
            _continue.SetEnabled(canContinue);
            _main.SetMessage(message);
            Ui.Show(_main.Element, true);
        }

        public void ShowPause(string? message)
        {
            HideAll();
            _pause.SetMessage(message);
            Ui.Show(_pause.Element, true);
        }

        public void ShowDeath()
        {
            HideAll();
            Ui.Show(_death.Element, true);
        }

        public void HideAll()
        {
            Ui.Show(_main.Element, false);
            Ui.Show(_pause.Element, false);
            Ui.Show(_death.Element, false);
        }
    }
}
