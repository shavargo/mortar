using UnityEngine;

namespace Mortar.Unity
{
    public static class GlyphRules
    {
        public const int Blank = 0;

        public static int RampSlot(float luminance, float darkThreshold)
        {
            if (luminance < darkThreshold)
            {
                return Blank;
            }
            var span = (luminance - darkThreshold) / Mathf.Max(1e-6f, 1f - darkThreshold);
            return Mathf.Min(AsciiSettings.RampSize - 1, 1 + (int)(span * (AsciiSettings.RampSize - 1)));
        }

        public static int EdgeSlot(float gx, float gy, float threshold)
        {
            if (Mathf.Sqrt(gx * gx + gy * gy) < threshold)
            {
                return Blank;
            }
            var angle = Mathf.Repeat(Mathf.Atan2(gy, gx) * Mathf.Rad2Deg + 90f, 180f);
            return AsciiSettings.FirstEdgeSlot + Mathf.RoundToInt(angle / 45f) % 4;
        }
    }
}
