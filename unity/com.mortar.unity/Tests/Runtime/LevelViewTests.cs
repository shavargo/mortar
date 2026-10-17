using System.Collections;
using System.Linq;
using Mortar.Core.Model;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class LevelViewTests
    {
        private static readonly string[] TownNpcProps = { "Prop smith", "Prop healer", "Prop sage" };

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
        public void TownGrassAndWaterUseSeparateStrokeTilemaps()
        {
            var world = TestContent.NewSession().World;

            Show(world);

            var cells = world.Level.Layout.Cells;
            var grass = 0;
            var water = 0;
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    var ground = cells.GroundAt(x, y);
                    if (LevelView.IsGrass(ground))
                    {
                        grass++;
                    }
                    if (LevelView.IsWater(ground))
                    {
                        water++;
                    }
                }
            }
            Assert.Greater(grass, 0, "the town should have grass");
            Assert.Greater(water, 0, "the town should have water");
            Assert.AreEqual(grass, _view.GrassTileCount);
            Assert.AreEqual(water, _view.WaterTileCount);
            Assert.AreEqual(grass + water, _view.StrokeTileCount);
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

        [UnityTest]
        public IEnumerator ShowingTheNextLevelReplacesTheTown()
        {
            var host = new GameObject("game");
            host.transform.SetParent(_root.transform);
            var game = host.AddComponent<MortarGame>();
            game.Begin(TestContent.NewSession());
            var world = game.Session!.World;
            Show(world);
            var townNpcs = _view.Props.Where(prop => TownNpcProps.Contains(prop.name)).ToList();
            Assert.AreEqual(TownNpcProps.Length, townNpcs.Count);

            Journeys.TakeStairsDown(game);
            Show(world);
            yield return null;

            Assert.AreEqual(NonVoidCells(world.Level), _view.TileCount);
            Assert.AreEqual(SightBlockingCells(world.Level), _view.RaisedTileCount);
            var names = _view.Props.Select(prop => prop.name).ToList();
            Assert.Contains("Prop entrance", names);
            Assert.IsFalse(names.Any(name => TownNpcProps.Contains(name)));
            Assert.IsTrue(townNpcs.All(prop => prop == null), "town NPC props were not destroyed");
        }

        [Test]
        public void PropsSitOnTheirMarkerCells()
        {
            var world = TestContent.NewSession().World;

            Show(world);

            var exit = _view.Props.First(prop => prop.name == "Prop exit");
            var expected = _view.Space.Center(Journeys.MarkerCell(world, MarkerKinds.Exit)) + Vector3.up * MarkerProps.PropLift;
            Assert.AreEqual(expected, exit.transform.position);
        }

        private void Show(World world) =>
            _view.Show(world.Level, LevelView.TilesetFor(TestContent.Content, world.Level), StairsWalk.HasLevelAbove(world));

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

        private static int SightBlockingCells(LevelState level)
        {
            var tileset = LevelView.TilesetFor(TestContent.Content, level);
            var cells = level.Layout.Cells;
            var count = 0;
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    var index = tileset.IndexOf(cells.GroundAt(x, y));
                    if (index >= 0 && tileset.Grounds[index].BlocksSight)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
    }
}
