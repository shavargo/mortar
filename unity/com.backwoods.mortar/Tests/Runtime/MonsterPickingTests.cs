using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class MonsterPickingTests
    {
        private const float UpperBodyHeight = 66f / GeneratedSprites.PixelsPerUnit;

        private GameObject _host = null!;
        private SpriteCatalog _catalog = null!;
        private MortarApp _app = null!;
        private string _folder = "";

        [SetUp]
        public void SetUp()
        {
            _folder = TestPaths.FreshSaveFolder();
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _host = new GameObject("monster-picking-test");
            _host.SetActive(false);
            _app = _host.AddComponent<MortarApp>();
            _app.Configure(_catalog, UiRoot.CreatePanelSettings(null), _folder);
            _host.SetActive(true);
            _app.StartNewGame();
            Journeys.TakeStairsDown(_app.Game);
            _app.Actors.RenderNow();
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
        public void ClickingAMonstersUpperBodyAttacksIt()
        {
            var (monster, point) = UpperBodyOfAMonster();
            Assert.AreNotEqual(monster.Cell, _app.Level.Space.CellAt(point), "the point should be off the monster's cell");

            Assert.AreEqual(ClickKind.Attack, _app.Controls.ClickAt(point));
            _app.Game.RunTick();

            Assert.AreEqual(IntentKind.Attack, World.Player.Intent);
            Assert.AreEqual(monster.Id, World.Player.IntentTargetId);
        }

        [Test]
        public void HoverNamesTheMonsterUnderThePointer()
        {
            var (monster, point) = UpperBodyOfAMonster();
            var picked = _app.Actors.LivingMonsterAt(World, point, _app.CameraRig.Camera.transparencySortAxis);

            var text = HoverText.Describe(World, _app.Level.Space.CellAt(point), picked);

            Assert.AreEqual(monster.Id, picked);
            StringAssert.StartsWith(World.Content.Monster(monster.DefinitionId).Name, text);
        }

        private (Actor Monster, Vector3 Point) UpperBodyOfAMonster()
        {
            foreach (var actor in World.Level.Actors)
            {
                if (actor.Kind != ActorKind.Monster || !actor.IsAlive)
                {
                    continue;
                }
                var body = _app.Actors.Find(actor.Id)!.Body.transform;
                var point = body.TransformPoint(new Vector3(0f, UpperBodyHeight, 0f));
                if (!CoveredByAnotherMonster(actor.Id, point))
                {
                    return (actor, point);
                }
            }
            Assert.Fail("no unobstructed living monster on the level");
            return default;
        }

        private bool CoveredByAnotherMonster(int monsterId, Vector3 point)
        {
            foreach (var actor in World.Level.Actors)
            {
                if (actor.Id != monsterId && actor.Kind == ActorKind.Monster && _app.Actors.Find(actor.Id)!.Covers(point))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
