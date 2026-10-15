using System.Collections.Generic;
using Mortar.Game;
using Mortar.Game.Commands;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ClickResolverTests
    {
        private static readonly Cell Spot = new Cell(3, 4);

        [Test]
        public void LivingMonsterWinsOverEverythingElse()
        {
            var query = new FakeQuery { Monster = 9, Item = 5, Stairs = true, Walkable = true };

            var action = ClickResolver.Resolve(query, Spot)!.Value;

            Assert.AreEqual(ClickKind.Attack, action.Kind);
            Assert.AreEqual(9, action.TargetId);
        }

        [Test]
        public void GroundItemWinsOverStairsAndFloor()
        {
            var query = new FakeQuery { Item = 5, Stairs = true, Walkable = true };

            Assert.AreEqual(ClickKind.PickUp, ClickResolver.Resolve(query, Spot)!.Value.Kind);
        }

        [Test]
        public void StairsWinOverFloor()
        {
            var query = new FakeQuery { Stairs = true, Walkable = true };

            Assert.AreEqual(ClickKind.Stairs, ClickResolver.Resolve(query, Spot)!.Value.Kind);
        }

        [Test]
        public void WalkableFloorMoves()
        {
            var action = ClickResolver.Resolve(new FakeQuery { Walkable = true }, Spot)!.Value;

            Assert.AreEqual(ClickKind.Move, action.Kind);
            Assert.AreEqual(Spot, action.Cell);
        }

        [Test]
        public void WallsResolveToNothing()
        {
            Assert.IsNull(ClickResolver.Resolve(new FakeQuery(), Spot));
        }

        [Test]
        public void ActionsBecomeTheMatchingCommands()
        {
            Assert.IsInstanceOf<AttackCommand>(ClickResolver.ToCommand(new ClickAction(ClickKind.Attack, Spot, 9)));
            Assert.IsInstanceOf<PickUpCommand>(ClickResolver.ToCommand(new ClickAction(ClickKind.PickUp, Spot, 5)));
            var move = (MoveCommand)ClickResolver.ToCommand(new ClickAction(ClickKind.Move, Spot, 0));
            Assert.AreEqual(Spot, move.Destination);
        }

        private sealed class FakeQuery : ICellQuery
        {
            public int Monster { get; set; }

            public int Item { get; set; }

            public bool Stairs { get; set; }

            public bool Walkable { get; set; }

            public int LivingMonsterAt(Cell cell) => Monster;

            public int GroundItemAt(Cell cell) => Item;

            public bool IsStairs(Cell cell) => Stairs;

            public bool IsWalkable(Cell cell) => Walkable;
        }
    }
}
