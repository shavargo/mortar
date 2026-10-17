using UnityEngine;

namespace Mortar.Unity
{
    public readonly struct AsciiLight
    {
        public AsciiLight(Vector3 position, float radius, float intensity)
        {
            Position = position;
            Radius = radius;
            Intensity = intensity;
        }

        public Vector3 Position { get; }

        public float Radius { get; }

        public float Intensity { get; }
    }
}
