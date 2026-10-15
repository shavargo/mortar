using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mortar.Unity
{
    [Serializable]
    public sealed class SpriteEntry
    {
        [SerializeField] private string _key = "";
        [SerializeField] private Sprite? _sprite;

        public SpriteEntry(string key, Sprite sprite)
        {
            _key = key;
            _sprite = sprite;
        }

        public string Key => _key;

        public Sprite? Sprite => _sprite;
    }

    [CreateAssetMenu(menuName = "Mortar/Sprite Catalog", fileName = "SpriteCatalog")]
    public sealed class SpriteCatalog : ScriptableObject
    {
        [SerializeField] private List<SpriteEntry> _entries = new List<SpriteEntry>();

        private readonly HashSet<string> _warned = new HashSet<string>();
        private Dictionary<string, Sprite>? _lookup;

        public int Count => _entries.Count;

        public IReadOnlyList<SpriteEntry> Entries => _entries;

        public void SetEntries(IEnumerable<SpriteEntry> entries)
        {
            _entries = entries.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToList();
            _lookup = null;
            _warned.Clear();
        }

        public Sprite Tile(string theme, string ground, int variant, bool raised)
        {
            var key = SpriteKeys.Tile(theme, ground, variant);
            var sprite = Find(key);
            if (sprite == null)
            {
                sprite = Find(SpriteKeys.Tile(theme, ground, 0));
            }
            return sprite != null ? sprite : Missing(key, raised ? GeneratedSprites.Raised : GeneratedSprites.Flat);
        }

        public Sprite Actor(string spriteSet) => Resolve(SpriteKeys.Actor(spriteSet), GeneratedSprites.Actor);

        public Sprite Icon(string iconId) => Resolve(SpriteKeys.Icon(iconId), GeneratedSprites.Icon);

        public bool Has(string key) => Find(key) != null;

        private Sprite Resolve(string key, Sprite placeholder)
        {
            var sprite = Find(key);
            return sprite != null ? sprite : Missing(key, placeholder);
        }

        private Sprite? Find(string key)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach (var entry in _entries)
                {
                    if (entry.Sprite != null && entry.Key.Length > 0)
                    {
                        _lookup[entry.Key] = entry.Sprite;
                    }
                }
            }
            return _lookup.TryGetValue(key, out var sprite) ? sprite : null;
        }

        private Sprite Missing(string key, Sprite placeholder)
        {
            if (_warned.Add(key))
            {
                Debug.LogWarning($"Mortar: no sprite for '{key}', using a placeholder");
            }
            return placeholder;
        }

        private void OnValidate() => _lookup = null;
    }
}
