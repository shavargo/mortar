using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class ActorViewsTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private MortarGame _game = null!;
        private CellSpace _space = null!;
        private ActorViews _actors = null!;
        private GroundItemViews _items = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("actor-views-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _game = _root.AddComponent<MortarGame>();
            _space = new CellSpace(CellSpace.CreateGrid(_root.transform));
            _actors = ActorViews.Create(_root.transform, _game);
            _items = GroundItemViews.Create(_root.transform);
            _game.Begin(TestContent.NewSession());
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
        }

        private World World => _game.Session!.World;

        [Test]
        public void TownHasOneViewForThePlayer()
        {
            _actors.Sync(World, _space, _catalog);

            Assert.AreEqual(1, _actors.Count);
            Assert.IsNotNull(_actors.Find(Actor.PlayerId));
        }

        [Test]
        public void DeeperLevelHasAViewPerActor()
        {
            Journeys.TakeStairsDown(_game);
            _actors.Clear();

            _actors.Sync(World, _space, _catalog);

            Assert.AreEqual(World.Level.Actors.Count, _actors.Count);
            Assert.Greater(_actors.Count, 1);
        }

        [Test]
        public void PlayerViewSettlesOnThePlayerCellAfterMoving()
        {
            var start = World.Player.Cell;
            _game.Enqueue(new MoveCommand(new Cell(start.X + 1, start.Y)));
            for (var i = 0; i < 30; i++)
            {
                _game.RunTick();
                _actors.Sync(World, _space, _catalog);
            }

            var view = _actors.Find(Actor.PlayerId)!;
            view.Render(0f, Time.time);

            var expected = _space.Center(World.Player.Cell);
            Assert.AreEqual(expected, view.transform.position);
            Assert.AreEqual(expected, view.Body.transform.position);
        }

        [Test]
        public void ActorBodiesAreScaledDownAboutTheirFeet()
        {
            _actors.Sync(World, _space, _catalog);
            var view = _actors.Find(Actor.PlayerId)!;
            view.Render(0f, Time.time);

            var body = view.Body;
            Assert.Less(body.bounds.size.x, body.sprite.bounds.size.x);
            Assert.AreEqual(ActorView.BodyScale * body.sprite.bounds.size.x, body.bounds.size.x, 1e-4f);
            Assert.AreEqual(_space.Center(World.Player.Cell), body.transform.position);
        }

        [Test]
        public void DroppedItemGetsAGroundView()
        {
            var potion = World.Belongings.Belt.Get(0)!;
            _game.Enqueue(new DropItemCommand(potion.Id));
            _game.RunTick();

            _items.Sync(World.Level, _space, _catalog);

            Assert.AreEqual(1, World.Level.GroundItems.Count);
            Assert.AreEqual(1, _items.Count);
        }
    }
}
