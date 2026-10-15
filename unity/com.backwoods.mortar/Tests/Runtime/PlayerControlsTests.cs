using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class PlayerControlsTests
    {
        private GameObject _root = null!;
        private MortarGame _game = null!;
        private PlayerControls _input = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("player-input-test");
            _game = _root.AddComponent<MortarGame>();
            var space = new CellSpace(CellSpace.CreateGrid(_root.transform));
            _input = PlayerControls.Create(_root.transform, _game, () => space, null);
            _game.Begin(TestContent.NewSession());
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_root);

        private World World => _game.Session!.World;

        [Test]
        public void ClickingFloorWalksThePlayerThere()
        {
            var target = FindFloorAway(World, 3);

            Assert.AreEqual(ClickKind.Move, _input.Click(target));
            RunUntil(() => World.Player.Cell == target && StairsWalk.IsSettled(World.Player));

            Assert.AreEqual(target, World.Player.Cell);
        }

        [Test]
        public void ClickingStairsWalksThereAndDescends()
        {
            var exit = Journeys.MarkerCell(World, MarkerKinds.Exit);

            Assert.AreEqual(ClickKind.Stairs, _input.Click(exit));
            Assert.IsTrue(_input.HasPendingStairs);
            RunUntil(() => World.Depth == 1);

            Assert.AreEqual(1, World.Depth);
            Assert.IsFalse(_input.HasPendingStairs);
        }

        [Test]
        public void PendingStairsDoNotCarryIntoTheNextSession()
        {
            var folder = TestPaths.FreshSaveFolder();
            try
            {
                Journeys.WalkNextTo(_game, MarkerKinds.Exit);
                var slot = new SaveSlot(folder);
                Assert.IsNull(slot.Write(World));
                Assert.AreEqual(ClickKind.Stairs, _input.Click(Journeys.MarkerCell(World, MarkerKinds.Exit)));

                var loaded = GameSession.Load(TestContent.Content, slot, out var error);
                Assert.IsNotNull(loaded, error);
                _game.Begin(loaded!);
                Assert.IsFalse(_input.HasPendingStairs);
                for (var i = 0; i < 5; i++)
                {
                    _game.RunTick();
                }

                Assert.AreEqual(0, World.Depth);
            }
            finally
            {
                TestPaths.Delete(folder);
            }
        }

        [Test]
        public void TheTownEntranceIsNotStairs()
        {
            var entrance = Journeys.MarkerCell(World, MarkerKinds.Entrance);

            Assert.IsFalse(new WorldCellQuery(World).IsStairs(entrance));
            Assert.AreNotEqual(ClickKind.Stairs, _input.Click(entrance));
        }

        [Test]
        public void ClickingAWallDoesNothing()
        {
            var wall = FindWall(World);

            Assert.IsNull(_input.Click(wall));
            _game.RunTick();

            Assert.AreEqual(IntentKind.None, World.Player.Intent);
        }

        private void RunUntil(System.Func<bool> done)
        {
            for (var i = 0; i < Journeys.MaxTicks && !done(); i++)
            {
                _game.RunTick();
            }
        }

        private static Cell FindFloorAway(World world, int distance)
        {
            var start = world.Player.Cell;
            foreach (var direction in Directions.All)
            {
                var cell = new Cell(start.X + direction.DeltaX() * distance, start.Y + direction.DeltaY() * distance);
                if (world.Level.IsFree(cell) && !new WorldCellQuery(world).IsStairs(cell))
                {
                    return cell;
                }
            }
            Assert.Fail("no free floor near the start");
            return start;
        }

        private static Cell FindWall(World world)
        {
            var cells = world.Level.Layout.Cells;
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    if (!cells.IsWalkable(x, y))
                    {
                        return new Cell(x, y);
                    }
                }
            }
            Assert.Fail("no wall cell in the town");
            return default;
        }
    }
}
