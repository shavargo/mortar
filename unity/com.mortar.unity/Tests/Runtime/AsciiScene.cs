using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class AsciiScene : IDisposable
    {
        public const int Width = 1200;
        public const int Height = 600;
        public const float PixelsPerUnit = 100f;
        public const int LitLevel = 60;

        private readonly GameObject _root = new GameObject("ascii-scene");
        private readonly List<Object> _owned = new List<Object>();
        private readonly RenderTexture _target = new RenderTexture(Width, Height, 24);
        private readonly Texture2D _white;
        private Color32[] _pixels = new Color32[0];

        public AsciiScene()
        {
            var go = new GameObject("ascii-camera");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = new Vector3(0f, 0f, -10f);
            Camera = go.AddComponent<Camera>();
            Camera.orthographic = true;
            Camera.orthographicSize = Height / PixelsPerUnit / 2f;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Color.black;
            Camera.targetTexture = _target;
            Camera.enabled = false;
            _white = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var pixels = new Color32[16];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }
            _white.SetPixels32(pixels);
            _white.Apply();
        }

        public Camera Camera { get; }

        public SpriteRenderer AddCells(int column, int row, int columns, int rows, Color color, Material? material = null, int order = 0)
        {
            var sprite = Sprite.Create(_white, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            _owned.Add(sprite);
            var size = new Vector2(columns * AsciiSettings.CellWidth, rows * AsciiSettings.CellHeight);
            var left = column * AsciiSettings.CellWidth - Width / 2f;
            var bottom = row * AsciiSettings.CellHeight - Height / 2f;
            var go = new GameObject($"cells {column},{row}");
            go.transform.SetParent(_root.transform, false);
            go.transform.position = new Vector3(left + size.x / 2f, bottom + size.y / 2f, 0f) / PixelsPerUnit;
            go.transform.localScale = new Vector3(size.x / PixelsPerUnit, size.y / PixelsPerUnit, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }
            return renderer;
        }

        public void Render()
        {
            Camera.Render();
            _pixels = Read(_target);
        }

        public Color32 Pixel(int x, int y) => _pixels[y * Width + x];

        public int LitPixels(int column, int row)
        {
            var count = 0;
            ForEachPixel(column, row, (x, y, pixel) => count += IsLit(pixel) ? 1 : 0);
            return count;
        }

        public Vector2Int InkSpan(int column, int row)
        {
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            ForEachPixel(column, row, (x, y, pixel) =>
            {
                if (!IsLit(pixel)) return;
                min = Vector2Int.Min(min, new Vector2Int(x, y));
                max = Vector2Int.Max(max, new Vector2Int(x, y));
            });
            return max.x < min.x ? Vector2Int.zero : max - min + Vector2Int.one;
        }

        public Color32[] Read(RenderTexture texture)
        {
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            var pixels = image.GetPixels32();
            Object.Destroy(image);
            return pixels;
        }

        public void Dispose()
        {
            Camera.targetTexture = null;
            Object.DestroyImmediate(_root);
            _target.Release();
            Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_white);
            foreach (var item in _owned)
            {
                Object.DestroyImmediate(item);
            }
            _owned.Clear();
        }

        private static bool IsLit(Color32 pixel) => Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) > LitLevel;

        private void ForEachPixel(int column, int row, Action<int, int, Color32> visit)
        {
            for (var y = row * AsciiSettings.CellHeight; y < (row + 1) * AsciiSettings.CellHeight; y++)
            {
                for (var x = column * AsciiSettings.CellWidth; x < (column + 1) * AsciiSettings.CellWidth; x++)
                {
                    visit(x, y, Pixel(x, y));
                }
            }
        }
    }
}
