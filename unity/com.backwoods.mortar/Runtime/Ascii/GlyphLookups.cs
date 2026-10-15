using UnityEngine;

namespace Mortar.Unity
{
    public static class GlyphLookups
    {
        public const int RampWidth = 256;
        public const int EdgeSize = 64;
        public const int SlotStep = 255 / (AsciiSettings.Slots - 1);

        public static Texture2D Ramp(float darkThreshold)
        {
            var values = new byte[RampWidth];
            for (var i = 0; i < RampWidth; i++)
            {
                values[i] = Encode(GlyphRules.RampSlot((i + 0.5f) / RampWidth, darkThreshold));
            }
            return Create(RampWidth, 1, values, FilterMode.Point, "Mortar Ramp Lookup");
        }

        public static Texture2D Edges(float threshold)
        {
            var values = new byte[EdgeSize * EdgeSize];
            for (var y = 0; y < EdgeSize; y++)
            {
                for (var x = 0; x < EdgeSize; x++)
                {
                    values[y * EdgeSize + x] = Encode(GlyphRules.EdgeSlot(Gradient(x), Gradient(y), threshold));
                }
            }
            return Create(EdgeSize, EdgeSize, values, FilterMode.Point, "Mortar Edge Lookup");
        }

        public static float Gradient(int texel) => (texel + 0.5f) / EdgeSize * 2f - 1f;

        public static Texture2D Create(int width, int height, byte[] values, FilterMode filter, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.R8, false, true)
            {
                name = name,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixelData(values, 0);
            texture.Apply(false, false);
            return texture;
        }

        private static byte Encode(int slot) => (byte)(slot * SlotStep);
    }
}
