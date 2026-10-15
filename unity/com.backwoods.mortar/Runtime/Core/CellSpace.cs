using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class CellSpace
    {
        private readonly Grid _grid;

        public CellSpace(Grid grid)
        {
            _grid = grid;
        }

        public Vector3 Center(Cell cell)
        {
            var world = _grid.GetCellCenterWorld(IsoMath.ToTileCell(cell));
            return new Vector3(world.x, world.y, 0f);
        }

        public Cell CellAt(Vector3 world) => IsoMath.FromTileCell(_grid.WorldToCell(new Vector3(world.x, world.y, 0f)));

        public static Grid CreateGrid(Transform parent)
        {
            var go = new GameObject("Grid");
            go.transform.SetParent(parent, false);
            var grid = go.AddComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
            return grid;
        }
    }
}
