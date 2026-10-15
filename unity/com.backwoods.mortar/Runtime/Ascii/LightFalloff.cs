using UnityEngine;

namespace Mortar.Unity
{
    public static class LightFalloff
    {
        public static float At(float distance)
        {
            var t = Mathf.Clamp01((distance - AsciiSettings.LightInner) / (1f - AsciiSettings.LightInner));
            return 1f - t * t * (3f - 2f * t);
        }
    }
}
