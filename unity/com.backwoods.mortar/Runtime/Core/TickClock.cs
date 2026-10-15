using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class TickClock
    {
        public const float TickSeconds = 1f / GameClock.TicksPerSecond;
        public const int MaxTicksPerFrame = 5;

        private float _accumulator;

        public float Alpha => _accumulator / TickSeconds;

        public int Advance(float deltaSeconds)
        {
            _accumulator += Mathf.Max(0f, deltaSeconds);
            var ticks = 0;
            while (_accumulator >= TickSeconds && ticks < MaxTicksPerFrame)
            {
                _accumulator -= TickSeconds;
                ticks++;
            }
            if (_accumulator >= TickSeconds)
            {
                _accumulator %= TickSeconds;
            }
            return ticks;
        }

        public void Reset() => _accumulator = 0f;
    }
}
