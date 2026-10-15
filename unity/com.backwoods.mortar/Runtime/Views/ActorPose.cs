using Mortar.Game;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ActorPose
    {
        public const float LungeDistance = 0.25f;
        public const float BobHeight = 0.06f;

        public static float Progress(int elapsed, int duration, float alpha) =>
            duration <= 0 ? 1f : Mathf.Clamp01((elapsed + alpha) / duration);

        public static Vector3 Ground(ActionKind action, float progress, Vector3 from, Vector3 to) =>
            action == ActionKind.Moving ? Vector3.Lerp(from, to, progress) : to;

        public static Vector3 Body(ActionKind action, float progress, Vector3 from, Vector3 to, Vector3 target)
        {
            var arc = Mathf.Sin(Mathf.PI * progress);
            switch (action)
            {
                case ActionKind.Moving:
                    return Ground(action, progress, from, to) + Vector3.up * (BobHeight * arc);
                case ActionKind.Attacking:
                    var toward = target - to;
                    return toward.sqrMagnitude < 1e-6f ? to : to + toward.normalized * (LungeDistance * arc);
                default:
                    return to;
            }
        }

        public static bool FacesLeft(Direction facing) => IsoMath.ScreenDirection(facing).x < -0.01f;

        public static float ArrowDegrees(Direction facing)
        {
            var direction = IsoMath.ScreenDirection(facing);
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }
}
