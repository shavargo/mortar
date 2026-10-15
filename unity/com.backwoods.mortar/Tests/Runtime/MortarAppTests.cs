using Mortar.Game.Content;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class MortarAppTests
    {
        private GameObject _host = null!;
        private SpriteCatalog _catalog = null!;
        private MortarApp _app = null!;
        private string _folder = "";

        [SetUp]
        public void SetUp()
        {
            _folder = TestPaths.FreshSaveFolder();
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _host = new GameObject("app-test");
            _host.SetActive(false);
            _app = _host.AddComponent<MortarApp>();
            _app.Configure(_catalog, UiRoot.CreatePanelSettings(null), _folder);
            _host.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
            Object.Destroy(_catalog);
            TestPaths.Delete(_folder);
        }

        private World World => _app.Game.Session!.World;

        [Test]
        public void StartsOnTheMainMenu()
        {
            Assert.IsNull(_app.LoadError);
            Assert.IsTrue(_app.Menus.IsMainOpen);
            Assert.IsFalse(_app.CanContinue);
        }

        [Test]
        public void NewGameShowsTheTownAndHud()
        {
            _app.StartNewGame();
            _app.RefreshFrame();

            Assert.IsFalse(_app.Menus.AnyOpen);
            Assert.AreEqual(0, World.Depth);
            Assert.Greater(_app.Level.TileCount, 0);
            Assert.IsNotNull(_app.Actors.Find(Actor.PlayerId));
            Assert.AreEqual($"{World.Player.Life}/{World.Player.Profile.MaxLife}", _app.Hud.Life.Text);
        }

        [Test]
        public void SaveThenContinueRestoresTheSameWorld()
        {
            _app.StartNewGame();
            _app.TogglePause();
            _app.SaveGame();
            var hash = WorldHash.Compute(World);

            Assert.AreEqual("Game saved", _app.Menus.PauseMessage);
            _app.QuitToMenu();
            Assert.IsTrue(_app.CanContinue);
            _app.ContinueGame();

            Assert.AreEqual(hash, WorldHash.Compute(World));
            Assert.IsFalse(_app.Game.Paused);
        }

        [Test]
        public void DescendingRebuildsTheLevelAroundThePlayer()
        {
            _app.StartNewGame();

            Journeys.TakeStairsDown(_app.Game);
            _app.Actors.RenderNow();

            Assert.AreEqual(1, World.Depth);
            Assert.IsTrue(_app.CanContinue);
            var view = _app.Actors.Find(Actor.PlayerId)!;
            var center = _app.Level.Space.Center(World.Player.Cell);
            Assert.Less(Vector2.Distance(view.transform.position, center), 0.01f);
        }

        [Test]
        public void TogglePauseShowsAndHidesThePauseMenu()
        {
            _app.StartNewGame();

            _app.TogglePause();
            Assert.IsTrue(_app.Game.Paused);
            Assert.IsTrue(_app.Menus.IsPauseOpen);

            _app.TogglePause();
            Assert.IsFalse(_app.Game.Paused);
            Assert.IsFalse(_app.Menus.AnyOpen);
        }

        [Test]
        public void CharacterSheetShowsBaseStats()
        {
            _app.StartNewGame();
            _app.Character.Open();

            _app.Character.Refresh(World);

            Assert.AreEqual($"Strength: {World.Progress.BaseStats.Strength}", _app.Character.StatText(StatKind.Strength));
            Assert.IsFalse(_app.Character.CanSpend);
        }
    }
}
