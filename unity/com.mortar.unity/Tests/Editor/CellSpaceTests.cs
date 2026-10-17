using Mortar.Game;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class CellSpaceTests
    {
        private GameObject _root = null!;
        private CellSpace _space = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("cell-space-test");
            _space = new CellSpace(CellSpace.CreateGrid(_root.transform));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void CellCentersRoundTrip()
        {
            foreach (var cell in new[] { new Cell(0, 0), new Cell(5, 3), new Cell(12, 40), new Cell(63, 1) })
            {
                Assert.AreEqual(cell, _space.CellAt(_space.Center(cell)));
            }
        }

        [Test]
        public void EastIsDownRightAndSouthIsDownLeft()
        {
            var origin = _space.Center(new Cell(4, 4));
            var east = _space.Center(new Cell(5, 4));
            var south = _space.Center(new Cell(4, 5));

            Assert.AreEqual(0.5f, east.x - origin.x, 1e-4f);
            Assert.AreEqual(-0.25f, east.y - origin.y, 1e-4f);
            Assert.AreEqual(-0.5f, south.x - origin.x, 1e-4f);
            Assert.AreEqual(-0.25f, south.y - origin.y, 1e-4f);
        }

        [Test]
        public void CentersLieOnTheZeroPlane()
        {
            Assert.AreEqual(0f, _space.Center(new Cell(7, 9)).z);
        }

        [Test]
        public void ScreenDirectionOfEastPointsDownRightWithUnitLength()
        {
            var east = IsoMath.ScreenDirection(Direction.East);

            Assert.Greater(east.x, 0f);
            Assert.Less(east.y, 0f);
            Assert.AreEqual(1f, east.magnitude, 1e-4f);
        }
    }
}
