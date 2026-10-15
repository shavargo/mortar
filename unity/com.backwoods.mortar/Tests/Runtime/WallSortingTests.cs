using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class WallSortingTests
    {
        private const int Size = 256;

        private GameObject _host = null!;
        private SpriteCatalog _catalog = null!;
        private MortarApp _app = null!;
        private string _folder = "";

        [SetUp]
        public void SetUp()
        {
            _folder = TestPaths.FreshSaveFolder();
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _host = new GameObject("wall-sorting-test");
            _host.SetActive(false);
            _app = _host.AddComponent<MortarApp>();
            _app.Configure(_catalog, UiRoot.CreatePanelSettings(null), _folder);
            _host.SetActive(true);
            _app.StartNewGame();
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
        public void ActorInFrontOfAWallIsDrawnOverIt()
        {
            var wall = WalkNextToAWall(wallAbove: true);

            Assert.IsTrue(IsActorColor(PixelBetweenPlayerAnd(wall)), "the wall behind the player was drawn over it");
        }

        [Test]
        public void ActorBehindAWallIsDrawnUnderIt()
        {
            var wall = WalkNextToAWall(wallAbove: false);

            Assert.IsFalse(IsActorColor(PixelBetweenPlayerAnd(wall)), "the player was drawn over the wall in front of it");
        }

        private Cell WalkNextToAWall(bool wallAbove)
        {
            var (spot, wall) = FindSpotBeside(wallAbove);
            _app.Game.Enqueue(new MoveCommand(spot));
            for (var i = 0; i < Journeys.MaxTicks && !(World.Player.Cell == spot && StairsWalk.IsSettled(World.Player)); i++)
            {
                _app.Game.RunTick();
            }
            Assert.AreEqual(spot, World.Player.Cell, "the player did not reach the cell beside the wall");
            return wall;
        }

        private (Cell Spot, Cell Wall) FindSpotBeside(bool wallAbove)
        {
            var level = World.Level;
            var space = _app.Level.Space;
            var start = World.Player.Cell;
            var best = ((Cell Spot, Cell Wall)?)null;
            var cells = level.Layout.Cells;
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    var spot = new Cell(x, y);
                    if (!level.IsFree(spot) || new WorldCellQuery(World).IsStairs(spot))
                    {
                        continue;
                    }
                    foreach (var direction in new[] { Direction.North, Direction.East, Direction.South, Direction.West })
                    {
                        var wall = spot.Step(direction);
                        var above = space.Center(wall).y > space.Center(spot).y;
                        if (above == wallAbove && IsRaised(wall)
                            && (best == null || spot.DistanceTo(start) < best.Value.Spot.DistanceTo(start)))
                        {
                            best = (spot, wall);
                        }
                    }
                }
            }
            Assert.IsNotNull(best, "no free cell beside a wall in the town");
            return best!.Value;
        }

        private bool IsRaised(Cell cell)
        {
            if (!World.Level.InBounds(cell))
            {
                return false;
            }
            var tileset = LevelView.TilesetFor(TestContent.Content, World.Level);
            var index = tileset.IndexOf(World.Level.Layout.Cells.GroundAt(cell.X, cell.Y));
            return index >= 0 && tileset.Grounds[index].BlocksSight;
        }

        private Color32 PixelBetweenPlayerAnd(Cell wall)
        {
            _app.Actors.Flash(Actor.PlayerId);
            _app.Actors.RenderNow();
            _app.CameraRig.Snap();
            var feet = _app.Level.Space.Center(World.Player.Cell);
            var block = _app.Level.Space.Center(wall);
            var point = new Vector3(feet.x + 0.3f * (block.x - feet.x), Mathf.Max(feet.y, block.y) + 0.2f, 0f);
            return Render(_app.CameraRig.Camera, point);
        }

        private static Color32 Render(Camera camera, Vector3 point)
        {
            var target = new RenderTexture(Size, Size, 24);
            var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            var viewport = camera.WorldToViewportPoint(point);
            var pixel = image.GetPixel((int)(viewport.x * Size), (int)(viewport.y * Size));
            Object.Destroy(image);
            target.Release();
            Object.Destroy(target);
            return pixel;
        }

        private static bool IsActorColor(Color32 pixel) => pixel.r > 200 && pixel.g < 40 && pixel.b > 40 && pixel.b < 200;
    }
}
