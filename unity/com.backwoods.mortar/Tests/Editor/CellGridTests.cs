using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class CellGridTests
    {
        [TestCase(1920, 1080, 320, 108)]
        [TestCase(1280, 720, 214, 72)]
        [TestCase(1200, 600, 200, 60)]
        [TestCase(0, 0, 1, 1)]
        public void CellsCoverTheWholeImage(int width, int height, int columns, int rows)
        {
            var grid = new CellGrid(width, height);

            Assert.AreEqual(columns, grid.Columns);
            Assert.AreEqual(rows, grid.Rows);
        }
    }
}
