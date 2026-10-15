using System.Collections.Generic;

namespace Mortar.Unity
{
    public sealed class MessageLog
    {
        public const int Capacity = 4;
        public const float LifetimeSeconds = 4f;

        private readonly List<KeyValuePair<string, float>> _entries = new List<KeyValuePair<string, float>>();

        public void Add(string text, float now)
        {
            _entries.Add(new KeyValuePair<string, float>(text, now));
            if (_entries.Count > Capacity)
            {
                _entries.RemoveAt(0);
            }
        }

        public IReadOnlyList<string> Visible(float now)
        {
            var visible = new List<string>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (now - entry.Value < LifetimeSeconds)
                {
                    visible.Add(entry.Key);
                }
            }
            return visible;
        }

        public void Clear() => _entries.Clear();
    }
}
