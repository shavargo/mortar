using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class TestClips : IDisposable
    {
        private const int FrameWidth = 4;
        private const int FrameHeight = 6;

        private readonly List<Texture2D> _textures = new List<Texture2D>();

        public void InstallFighter(SpriteCatalog catalog, string spriteSet)
        {
            var clips = new List<ActorClip>(catalog.Clips)
            {
                Make(spriteSet, ClipNames.Idle, 4, 8, true, 0),
                Make(spriteSet, ClipNames.Walk, 8, 8, true, 0),
                Make(spriteSet, ClipNames.Attack, 6, 8, false, 3),
                Make(spriteSet, ClipNames.Hit, 3, 8, false, 0),
                Make(spriteSet, ClipNames.Death, 5, 8, false, 0),
            };
            catalog.SetClips(clips);
        }

        public void InstallNpc(SpriteCatalog catalog, string spriteSet)
        {
            var clips = new List<ActorClip>(catalog.Clips) { Make(spriteSet, ClipNames.Idle, 4, 1, true, 0) };
            catalog.SetClips(clips);
        }

        public void Dispose()
        {
            foreach (var texture in _textures)
            {
                Object.Destroy(texture);
            }
            _textures.Clear();
        }

        private ActorClip Make(string spriteSet, string name, int frames, int directions, bool loop, int hitFrame)
        {
            var texture = new Texture2D(frames * FrameWidth, directions * FrameHeight);
            _textures.Add(texture);
            var json = $"{{\"frames\":{frames},\"directions\":{directions},\"fps\":10,\"loop\":{(loop ? "true" : "false")}," +
                $"\"hitFrame\":{hitFrame},\"frameWidth\":{FrameWidth},\"frameHeight\":{FrameHeight},\"pivotX\":2,\"pivotY\":1}}";
            return new ActorClip(SpriteKeys.Clip(spriteSet, name), texture, ClipInfo.FromJson(json));
        }
    }
}
