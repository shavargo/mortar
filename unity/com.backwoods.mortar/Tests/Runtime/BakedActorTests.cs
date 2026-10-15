using System.Collections.Generic;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class BakedActorTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private TestClips _clips = null!;
        private MortarGame _game = null!;
        private CellSpace _space = null!;
        private ActorViews _actors = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("baked-actor-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _clips = new TestClips();
            _clips.InstallFighter(_catalog, "warrior");
            _game = _root.AddComponent<MortarGame>();
            _space = new CellSpace(CellSpace.CreateGrid(_root.transform));
            _actors = ActorViews.Create(_root.transform, _game);
            _game.Begin(TestContent.NewSession());
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
            _clips.Dispose();
        }

        private World World => _game.Session!.World;

        [Test]
        public void BakedActorsShowTheirIdleClipAtFullSize()
        {
            _actors.Sync(World, _space, _catalog);
            var view = _actors.Find(Actor.PlayerId)!;

            view.Render(0f, 0f);

            var idle = _catalog.Clip("warrior", ClipNames.Idle)!;
            Assert.IsTrue(view.IsBaked);
            Assert.AreSame(idle.Frame(World.Player.Facing, 0), view.Body.sprite);
            Assert.AreEqual(Vector3.one, view.Body.transform.localScale);
            Assert.AreEqual(Vector3.zero, view.Body.transform.localPosition);
            Assert.IsFalse(view.Body.flipX);
        }

        [Test]
        public void TheAttackHitFrameShowsOnTheHitTick()
        {
            Journeys.TakeStairsDown(_game);
            _actors.Clear();
            var player = World.Player;
            _game.Enqueue(new AttackCommand(NearestMonster().Id));
            for (var i = 0; i < Journeys.MaxTicks; i++)
            {
                _game.RunTick();
                _actors.Sync(World, _space, _catalog);
                if (player.Action == ActionKind.Attacking && player.ActionElapsed == player.Profile.Timings.HitFrame)
                {
                    var view = _actors.Find(Actor.PlayerId)!;
                    view.Render(0f, Time.time);
                    var attack = _catalog.Clip("warrior", ClipNames.Attack)!;
                    Assert.AreSame(attack.Frame(player.Facing, attack.HitFrame), view.Body.sprite);
                    return;
                }
            }
            Assert.Fail("the player never reached the hit tick of an attack");
        }

        [Test]
        public void TownNpcsWithAnIdleClipAreAnimated()
        {
            _clips.InstallNpc(_catalog, "smith");
            var props = new List<GameObject>();

            MarkerProps.Place(World.Level, _space, _catalog, _root.transform, false, props);

            var smith = props.Find(prop => prop.name == "Prop smith");
            Assert.IsNotNull(smith, "town should have a smith marker");
            var animator = smith!.GetComponent<PropAnimator>();
            Assert.IsTrue(animator != null, "the smith prop should have a PropAnimator");
            animator!.Show(0.25f);
            var idle = _catalog.Clip("smith", ClipNames.Idle)!;
            Assert.AreSame(idle.Frame(Direction.South, 2), smith.GetComponent<SpriteRenderer>().sprite);
        }

        private Actor NearestMonster()
        {
            Actor? nearest = null;
            foreach (var actor in World.Level.Actors)
            {
                if (actor.Kind == ActorKind.Monster && actor.IsAlive
                    && (nearest == null || actor.Cell.DistanceTo(World.Player.Cell) < nearest.Cell.DistanceTo(World.Player.Cell)))
                {
                    nearest = actor;
                }
            }
            Assert.IsNotNull(nearest, "the level should have a living monster");
            return nearest!;
        }
    }
}
