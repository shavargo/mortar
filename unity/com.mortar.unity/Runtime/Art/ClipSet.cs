using System;
using System.Collections.Generic;

namespace Mortar.Unity
{
    public sealed class ClipSet
    {
        private static readonly string[] Names =
        {
            ClipNames.Idle, ClipNames.Walk, ClipNames.Attack, ClipNames.Hit, ClipNames.Death,
        };

        private readonly Dictionary<string, ActorClip> _clips;

        public ClipSet(IReadOnlyDictionary<string, ActorClip> clips)
        {
            if (!clips.ContainsKey(ClipNames.Idle))
            {
                throw new ArgumentException("a clip set needs an idle clip", nameof(clips));
            }
            _clips = new Dictionary<string, ActorClip>(clips, StringComparer.Ordinal);
        }

        public static ClipSet? For(SpriteCatalog catalog, string spriteSet)
        {
            var found = new Dictionary<string, ActorClip>(StringComparer.Ordinal);
            foreach (var name in Names)
            {
                var clip = catalog.Clip(spriteSet, name);
                if (clip != null)
                {
                    found.Add(name, clip);
                }
            }
            return found.ContainsKey(ClipNames.Idle) ? new ClipSet(found) : null;
        }

        public bool Has(string name) => _clips.ContainsKey(name);

        public ActorClip Get(string name) => _clips.TryGetValue(name, out var clip) ? clip : _clips[ClipNames.Idle];
    }
}
