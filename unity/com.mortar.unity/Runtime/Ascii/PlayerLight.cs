using UnityEngine;

namespace Mortar.Unity
{
    public static class PlayerLight
    {
        public static readonly float RadiusUnits = AsciiSettings.LightRadiusTiles / Mathf.Sqrt(2f);

        public static void Apply(AsciiFilter filter, int depth, Vector3? player)
        {
            filter.Ambient = depth > 0 ? 0f : 1f;
            filter.Lights.Clear();
            if (player.HasValue)
            {
                filter.Lights.Add(new AsciiLight(player.Value, RadiusUnits, 1f));
            }
        }
    }
}
