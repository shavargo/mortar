using System;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    [DefaultExecutionOrder(100)]
    public sealed class MortarApp : MonoBehaviour
    {
        [SerializeField] private SpriteCatalog? _catalog;
        [SerializeField] private PanelSettings? _panelSettings;
        [SerializeField] private string _classId = "warrior";
        private string? _saveFolder;

        public MortarGame Game { get; private set; } = null!;

        public GameContent? Content { get; private set; }

        public string? LoadError { get; private set; }

        public bool CanContinue => Content != null && Game.Slot != null && Game.Slot.Exists;

        public LevelView Level { get; private set; } = null!;

        public ActorViews Actors { get; private set; } = null!;

        public GroundItemViews Items { get; private set; } = null!;

        public CameraFollow CameraRig { get; private set; } = null!;

        public PlayerControls Controls { get; private set; } = null!;

        public UiRoot Ui { get; private set; } = null!;

        public HudView Hud { get; private set; } = null!;

        public InventoryScreen Inventory { get; private set; } = null!;

        public CharacterSheet Character { get; private set; } = null!;

        public Menus Menus { get; private set; } = null!;

        public SessionPresenter Presenter { get; private set; } = null!;

        public void Configure(SpriteCatalog catalog, PanelSettings panelSettings, string? saveFolder)
        {
            _catalog = catalog;
            _panelSettings = panelSettings;
            _saveFolder = saveFolder;
        }

        public void StartNewGame()
        {
            if (Content == null)
            {
                Menus.ShowMain(LoadError, false);
                return;
            }
            var session = GameSession.NewGame(Content, _classId, unchecked((uint)Environment.TickCount), out var error);
            BeginOrReport(session, error);
        }

        public void ContinueGame()
        {
            if (Content == null || Game.Slot == null || !CanContinue)
            {
                ReportFailure(LoadError ?? "No saved game");
                return;
            }
            var session = GameSession.Load(Content, Game.Slot, out var error);
            BeginOrReport(session, error);
        }

        public void SaveGame() => Menus.ShowPause(Game.Save() ?? "Game saved");

        public void QuitToMenu()
        {
            Game.End();
            CloseScreens();
            Presenter.Reset();
            Hud.Show(false);
            Menus.ShowMain(LoadError, CanContinue);
        }

        public void TogglePause()
        {
            if (Game.Session == null || Game.Session.World.IsGameOver)
            {
                return;
            }
            Game.Paused = !Game.Paused;
            if (Game.Paused)
            {
                Menus.ShowPause(null);
            }
            else
            {
                Menus.HideAll();
            }
        }

        public void Quit() => Application.Quit();

        public void RefreshFrame()
        {
            var session = Game.Session;
            if (session == null)
            {
                return;
            }
            var world = session.World;
            var hovered = Controls.CellUnderPointer();
            var hover = hovered.HasValue ? HoverText.Describe(world, hovered.Value, Controls.MonsterUnderPointer()) : null;
            Hud.Refresh(world, Presenter.Messages.Visible(Time.time), hover);
            Inventory.Refresh(world.Belongings);
            Character.Refresh(world);
            Presenter.Popups.Update(CameraRig.Camera, Time.time);
        }

        private void Awake()
        {
            var catalog = _catalog != null ? _catalog : ScriptableObject.CreateInstance<SpriteCatalog>();
            var settings = _panelSettings != null ? _panelSettings : UiRoot.CreatePanelSettings(null);
            Game = gameObject.AddComponent<MortarGame>();
            Game.Slot = _saveFolder != null ? new SaveSlot(_saveFolder) : SaveSlot.InPersistentData();
            CameraRig = CameraFollow.Create(transform);
            Level = LevelView.Create(transform, catalog);
            Actors = ActorViews.Create(transform, Game);
            Items = GroundItemViews.Create(transform);
            Ui = UiRoot.Create(transform, settings);
            Hud = new HudView(Ui, catalog);
            Inventory = new InventoryScreen(Ui, Game, catalog);
            Character = new CharacterSheet(Ui, Game);
            Controls = PlayerControls.Create(transform, Game, () => Level.Space, CameraRig.Camera);
            Presenter = new SessionPresenter(this, catalog);
            Menus = new Menus(Ui, this);
            Wire();
            Hud.Show(false);
            Content = ContentLoading.Load(ContentPaths.Manifest, out var error);
            LoadError = error;
            Menus.ShowMain(LoadError, CanContinue);
        }

        private void Wire()
        {
            Controls.InventoryToggled += Inventory.Toggle;
            Controls.CharacterToggled += Character.Toggle;
            Controls.MenuRequested += OnMenuRequested;
            Controls.IsOverUi = Ui.IsPointerOverUi;
            Controls.InterceptWorldClick = Inventory.DropHeld;
            Controls.MonsterAt = MonsterAt;
            Hud.Belt.SlotClicked += slot => Game.Enqueue(Inventory.Cursor.ClickBelt(slot));
            Presenter.PlayerDied += OnPlayerDied;
            CameraRig.Follow(PlayerPosition);
        }

        private Vector3? PlayerPosition()
        {
            if (Game.Session == null)
            {
                return null;
            }
            var view = Actors.Find(Actor.PlayerId);
            return view != null ? view.GroundPosition : (Vector3?)null;
        }

        private int MonsterAt(Vector3 point)
        {
            var session = Game.Session;
            return session == null ? 0 : Actors.LivingMonsterAt(session.World, point, CameraRig.Camera.transparencySortAxis);
        }

        private void BeginOrReport(GameSession? session, string? error)
        {
            if (session == null)
            {
                ReportFailure(error);
                return;
            }
            Menus.HideAll();
            CloseScreens();
            Hud.Show(true);
            Game.Begin(session);
        }

        private void ReportFailure(string? message)
        {
            var session = Game.Session;
            if (session != null && !session.World.IsGameOver)
            {
                Game.Paused = true;
                CloseScreens();
                Menus.ShowPause(message);
                return;
            }
            if (session != null)
            {
                QuitToMenu();
            }
            Menus.ShowMain(message, CanContinue);
        }

        private void OnMenuRequested()
        {
            if (Inventory.IsOpen)
            {
                Inventory.Close();
            }
            else if (Character.IsOpen)
            {
                Character.Close();
            }
            else
            {
                TogglePause();
            }
        }

        private void OnPlayerDied()
        {
            Game.Paused = true;
            CloseScreens();
            Menus.ShowDeath();
        }

        private void CloseScreens()
        {
            Inventory.Close();
            Character.Close();
        }

        private void Update() => RefreshFrame();

        private void LateUpdate() => RefreshLights();

        public void RefreshLights()
        {
            var session = Game.Session;
            if (session == null)
            {
                CameraRig.Filter.Tramplers.Clear();
                return;
            }
            PlayerLight.Apply(CameraRig.Filter, session.World.Depth, PlayerPosition());
            GrassTrampers.Collect(CameraRig.Filter.Tramplers, session.World, Actors, Level);
        }
    }
}
