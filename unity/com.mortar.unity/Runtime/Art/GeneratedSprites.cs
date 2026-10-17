using System;
using UnityEngine;

namespace Mortar.Unity
{
    public static class GeneratedSprites
    {
        public const int PixelsPerUnit = 64;

        private static readonly Color32 Magenta = new Color32(255, 0, 255, 255);
        private static readonly Color32 DarkMagenta = new Color32(150, 0, 150, 255);
        private static Sprite? _flat;
        private static Sprite? _raised;
        private static Sprite? _actor;
        private static Sprite? _icon;
        private static Sprite? _arrow;

        public static Sprite Flat => Cached(ref _flat, () => Make(64, 32, new Vector2(0.5f, 0.5f), FlatPixel));

        public static Sprite Raised => Cached(ref _raised, () => Make(64, 64, new Vector2(0.5f, 0.25f), RaisedPixel));

        public static Sprite Actor => Cached(ref _actor, () => Make(64, 96, new Vector2(0.5f, 4f / 96f), ActorPixel));

        public static Sprite Icon => Cached(ref _icon, () => Make(64, 64, new Vector2(0.5f, 0.5f), IconPixel));

        public static Sprite Arrow => Cached(ref _arrow, () => Make(16, 16, new Vector2(0.5f, 0.5f), ArrowPixel));

        private static Sprite Cached(ref Sprite? field, Func<Sprite> create)
        {
            if (field == null)
            {
                field = create();
            }
            return field;
        }

        private static Sprite Make(int width, int height, Vector2 pivot, Func<int, int, Color32?> paint)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    pixels[y * width + x] = paint(x, y) ?? new Color32(0, 0, 0, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), pivot, PixelsPerUnit);
        }

        private static Color32? FlatPixel(int x, int y) =>
            Mathf.Abs(x - 31.5f) / 32f + Mathf.Abs(y - 15.5f) / 16f <= 1f ? Magenta : (Color32?)null;

        private static Color32? RaisedPixel(int x, int y)
        {
            var across = Mathf.Abs(x - 31.5f) / 32f;
            if (across + Mathf.Max(0f, Mathf.Abs(y - 31.5f) - 16f) / 16f > 1f)
            {
                return null;
            }
            return across + Mathf.Abs(y - 47.5f) / 16f <= 1f ? Magenta : DarkMagenta;
        }

        private static Color32? ActorPixel(int x, int y) => x >= 16 && x < 48 && y >= 4 && y < 80 ? Magenta : (Color32?)null;

        private static Color32? IconPixel(int x, int y) =>
            x < 2 || y < 2 || x > 61 || y > 61 ? DarkMagenta : Magenta;

        private static Color32? ArrowPixel(int x, int y) =>
            x >= 2 && Mathf.Abs(y - 7.5f) <= (14 - x) * 0.5f ? new Color32(255, 255, 255, 255) : (Color32?)null;
    }
}
