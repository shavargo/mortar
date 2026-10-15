using System.Linq;
using Mortar.Core.Model;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class LevelViewTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private LevelView _view = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("level-view-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _view = LevelView.Create(_root.transform, _catalog);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
        }

        [Test]
        public void TownShowsATileForEveryNonVoidCell()
        {
            var world = TestContent.NewSession().World;

            Show(world);

            Assert.AreEqual(NonVoidCells(world.Level), _view.TileCount);
        }

        [Test]
        public void TownShowsStairsDownAndNpcsButNoStairsUp()
        {
            var world = TestContent.NewSession().World;

            Show(world);

            var names = _view.Props.Select(prop => prop.name).ToList();
            Assert.Contains("Prop exit", names);
            Assert.Contains("Prop smith", names);
            Assert.Contains("Prop healer", names);
            Assert.Contains("Prop sage", names);
            Assert.IsFalse(names.Contains("Prop entrance"));
        }

        [Test]
        public void ShowingTheNextLevelReplacesTheTown()
        {
            var host = new GameObject("game");
            host.transform.SetParent(_root.transform);
            var game = host.AddComponent<MortarGame>();
            game.Begin(TestContent.NewSession());
            Journeys.TakeStairsDown(game);
            var world = game.Session!.World;

            Show(world);

            Assert.AreEqual(NonVoidCells(world.Level), _view.TileCount);
            Assert.Contains("Prop entrance", _view.Props.Select(prop => prop.name).ToList());
        }

        [Test]
        public void PropsSitOnTheirMarkerCells()
        {
            var world = TestContent.NewSession().World;

            Show(world);

            var exit = _view.Props.First(prop => prop.name == "Prop exit");
            var expected = _view.Space.Center(Journeys.MarkerCell(world, MarkerKinds.Exit));
            Assert.AreEqual(expected, exit.transform.position);
        }

        private void Show(World world) =>
            _view.Show(world.Level, LevelView.TilesetFor(TestContent.Content, world.Level), world.Depth > world.Content.ShallowestLevel);

        private static int NonVoidCells(LevelState level)
        {
            var cells = level.Layout.Cells;
            var count = 0;
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    if (cells.GroundAt(x, y) != Tileset.VoidId)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
    }
}
