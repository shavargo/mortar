using Mortar.Game.Content;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ExperienceBar
    {
        public static long? NextThreshold(PlayerProgress progress, GameRules rules) =>
            progress.Level >= rules.MaxLevel ? (long?)null : rules.ExperienceThresholds[progress.Level - 1];

        public static float Fraction(PlayerProgress progress, GameRules rules)
        {
            var next = NextThreshold(progress, rules);
            if (!next.HasValue)
            {
                return 1f;
            }
            var floor = progress.Level >= 2 ? rules.ExperienceThresholds[progress.Level - 2] : 0L;
            var span = next.Value - floor;
            return span <= 0 ? 1f : Mathf.Clamp01((float)(progress.Experience - floor) / span);
        }
    }
}
