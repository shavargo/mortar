using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    /// <summary>Maps Mortar cells (x east, y south) onto Unity isometric grid cells.</summary>
    public static class IsoMath
    {
        public static Vector3Int ToTileCell(Cell cell) => new Vector3Int(-cell.Y, -cell.X, 0);

        public static Cell FromTileCell(Vector3Int tile) => new Cell(-tile.y, -tile.x);

        public static Vector2 ScreenDirection(Direction direction)
        {
            var dx = direction.DeltaX();
            var dy = direction.DeltaY();
            return new Vector2((dx - dy) * 0.5f, -(dx + dy) * 0.25f).normalized;
        }
    }
}
