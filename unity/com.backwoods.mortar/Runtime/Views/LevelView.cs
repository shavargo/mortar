using System.Collections.Generic;
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Mortar.Unity
{
    public sealed class LevelView : MonoBehaviour
    {
        public const int FloorOrder = -10;
        public const int WorldOrder = 0;

        private static readonly HashSet<string> StrokeGrounds = new HashSet<string> { "grass", "water" };

        private readonly Dictionary<Sprite, Tile> _tiles = new Dictionary<Sprite, Tile>();
        private readonly List<GameObject> _props = new List<GameObject>();
        private SpriteCatalog _catalog = null!;
        private Tilemap _floor = null!;
        private Tilemap _strokes = null!;
        private Tilemap _raised = null!;

        public CellSpace Space { get; private set; } = null!;

        public int FloorTileCount => CountTiles(_floor);

        public int RaisedTileCount => CountTiles(_raised);

        public int StrokeTileCount => CountTiles(_strokes);

        public int TileCount => FloorTileCount + RaisedTileCount + StrokeTileCount;

        public IReadOnlyList<GameObject> Props => _props;

        public static LevelView Create(Transform parent, SpriteCatalog catalog)
        {
            var go = new GameObject("Level");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<LevelView>();
            view._catalog = catalog;
            var grid = CellSpace.CreateGrid(go.transform);
            view.Space = new CellSpace(grid);
            view._floor = CreateTilemap(grid.transform, "Floor", FloorOrder, TilemapRenderer.Mode.Chunk, null);
            view._strokes = CreateTilemap(grid.transform, "Strokes", FloorOrder, TilemapRenderer.Mode.Chunk, MaskMaterials.Stroke);
            view._raised = CreateTilemap(grid.transform, "Raised", WorldOrder, TilemapRenderer.Mode.Individual, null);
            return view;
        }

        public static Tileset TilesetFor(GameContent content, LevelState level) => content.Areas[level.Layout.AreaId].Tileset;

        public static bool IsStroke(string ground) => StrokeGrounds.Contains(ground);

        public void Show(LevelState level, Tileset tileset, bool hasLevelAbove)
        {
            Clear();
            var cells = level.Layout.Cells;
            var theme = TileTheme.For(level.Layout.AreaId);
            var floor = new TileBatch();
            var strokes = new TileBatch();
            var raised = new TileBatch();
            for (var y = 0; y < cells.Height; y++)
            {
                for (var x = 0; x < cells.Width; x++)
                {
                    var ground = cells.GroundAt(x, y);
                    if (ground == Tileset.VoidId)
                    {
                        continue;
                    }
                    var index = tileset.IndexOf(ground);
                    var isRaised = index >= 0 && tileset.Grounds[index].BlocksSight;
                    var variant = cells.TileAt(x, y) == ground ? cells.VariantAt(x, y) : 0;
                    var tile = TileFor(_catalog.Tile(theme, ground, variant, isRaised));
                    var batch = isRaised ? raised : IsStroke(ground) ? strokes : floor;
                    batch.Add(IsoMath.ToTileCell(new Cell(x, y)), tile);
                }
            }
            floor.ApplyTo(_floor);
            strokes.ApplyTo(_strokes);
            raised.ApplyTo(_raised);
            MarkerProps.Place(level, Space, _catalog, transform, hasLevelAbove, _props);
        }

        public void Clear()
        {
            _floor.ClearAllTiles();
            _strokes.ClearAllTiles();
            _raised.ClearAllTiles();
            foreach (var prop in _props)
            {
                Destroy(prop);
            }
            _props.Clear();
        }

        private Tile TileFor(Sprite sprite)
        {
            if (!_tiles.TryGetValue(sprite, out var tile))
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                _tiles.Add(sprite, tile);
            }
            return tile;
        }

        private void OnDestroy()
        {
            foreach (var tile in _tiles.Values)
            {
                Destroy(tile);
            }
            _tiles.Clear();
        }

        private static int CountTiles(Tilemap tilemap)
        {
            var count = 0;
            foreach (var position in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.HasTile(position))
                {
                    count++;
                }
            }
            return count;
        }

        private static Tilemap CreateTilemap(Transform parent, string name, int order, TilemapRenderer.Mode mode, Material? material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = mode;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            return tilemap;
        }

        private sealed class TileBatch
        {
            private readonly List<Vector3Int> _positions = new List<Vector3Int>();
            private readonly List<TileBase> _tiles = new List<TileBase>();

            public void Add(Vector3Int position, TileBase tile)
            {
                _positions.Add(position);
                _tiles.Add(tile);
            }

            public void ApplyTo(Tilemap tilemap) => tilemap.SetTiles(_positions.ToArray(), _tiles.ToArray());
        }
    }
}
