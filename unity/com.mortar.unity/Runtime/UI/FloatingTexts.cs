using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class FloatingTexts
    {
        public const float Lifetime = 0.8f;
        public const float StartHeight = 0.9f;
        public const float Rise = 0.6f;

        private readonly VisualElement _layer;
        private readonly List<Entry> _entries = new List<Entry>();

        public FloatingTexts(VisualElement layer)
        {
            _layer = layer;
        }

        public int Count => _entries.Count;

        public void Spawn(string text, Vector3 world, Color color, float now)
        {
            var label = Ui.Text(text, 16);
            label.style.color = color;
            label.style.position = Position.Absolute;
            _layer.Add(label);
            _entries.Add(new Entry(label, world, now));
        }

        public void Update(Camera? camera, float now)
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                var age = now - entry.Start;
                if (age >= Lifetime)
                {
                    entry.Label.RemoveFromHierarchy();
                    _entries.RemoveAt(i);
                    continue;
                }
                if (camera == null || _layer.panel == null)
                {
                    continue;
                }
                var height = StartHeight + Rise * age / Lifetime;
                var point = RuntimePanelUtils.CameraTransformWorldToPanel(_layer.panel, entry.World + Vector3.up * height, camera);
                entry.Label.style.left = point.x - 12f;
                entry.Label.style.top = point.y;
                entry.Label.style.opacity = 1f - age / Lifetime;
            }
        }

        public void Clear()
        {
            foreach (var entry in _entries)
            {
                entry.Label.RemoveFromHierarchy();
            }
            _entries.Clear();
        }

        private sealed class Entry
        {
            public Entry(Label label, Vector3 world, float start)
            {
                Label = label;
                World = world;
                Start = start;
            }

            public Label Label { get; }

            public Vector3 World { get; }

            public float Start { get; }
        }
    }
}
