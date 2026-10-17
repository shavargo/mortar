using System.Collections;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class MortarGameTests
    {
        private GameObject _host = null!;
        private MortarGame _game = null!;
        private string _folder = "";

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("mortar-game-test");
            _game = _host.AddComponent<MortarGame>();
            _folder = TestPaths.FreshSaveFolder();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_host);
            TestPaths.Delete(_folder);
        }

        [Test]
        public void TwentyFiftyMillisecondStepsRunTwentyTicks()
        {
            _game.Begin(TestContent.NewSession());

            for (var i = 0; i < 20; i++)
            {
                _game.Step(0.05f);
            }

            Assert.AreEqual(20, _game.Session!.World.TickCount);
        }

        [Test]
        public void PausedGameDoesNotTick()
        {
            _game.Begin(TestContent.NewSession());
            _game.Paused = true;

            _game.Step(1f);

            Assert.AreEqual(0, _game.Session!.World.TickCount);
        }

        [Test]
        public void TickedFiresOncePerTick()
        {
            var count = 0;
            _game.Ticked += events => count++;
            _game.Begin(TestContent.NewSession());

            _game.Step(0.15f);

            Assert.AreEqual(3, count);
        }

        [Test]
        public void SaveThenLoadRestoresIdenticalState()
        {
            _game.Slot = new SaveSlot(_folder);
            _game.Begin(TestContent.NewSession());
            var player = _game.Session!.World.Player.Cell;
            _game.Enqueue(new MoveCommand(new Cell(player.X + 1, player.Y)));
            for (var i = 0; i < 40; i++)
            {
                _game.RunTick();
            }

            Assert.IsNull(_game.Save());
            var loaded = GameSession.Load(TestContent.Content, _game.Slot, out var error);

            Assert.IsNotNull(loaded, error);
            Assert.AreEqual(WorldHash.Compute(_game.Session.World), WorldHash.Compute(loaded!.World));
        }

        [Test]
        public void ChangingLevelAutoSaves()
        {
            string? notice = null;
            _game.Notice += text => notice = text;
            _game.Slot = new SaveSlot(_folder);
            _game.Begin(TestContent.NewSession());

            Journeys.TakeStairsDown(_game);

            Assert.IsTrue(_game.Slot.Exists);
            Assert.AreEqual("Game saved", notice);
            var loaded = GameSession.Load(TestContent.Content, _game.Slot, out var error);
            Assert.AreEqual(1, loaded!.World.Depth, error);
        }

        [UnityTest]
        public IEnumerator UpdateAdvancesTicksInRealTime()
        {
            _game.Begin(TestContent.NewSession());

            yield return new WaitForSeconds(0.3f);

            Assert.Greater(_game.Session!.World.TickCount, 0);
        }
    }
}
