using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class TestClips : IDisposable
    {
        public const int OutlinedWidth = 64;
        public const int OutlinedHeight = 96;
        public const int OutlinedPivotX = 32;
        public const int OutlinedPivotY = 8;

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

        public void InstallOutlined(SpriteCatalog catalog, string spriteSet)
        {
            var texture = NewTexture(OutlinedWidth, OutlinedHeight * 8);
            var pixels = texture.GetPixels32();
            for (var y = 0; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var inside = x >= OutlinedWidth / 4 && x < OutlinedWidth * 3 / 4
                        && y % OutlinedHeight >= OutlinedHeight / 4 && y % OutlinedHeight < OutlinedHeight * 3 / 4;
                    pixels[y * texture.width + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            var json = Json(1, 8, true, 0, OutlinedWidth, OutlinedHeight, OutlinedPivotX, OutlinedPivotY);
            var idle = new ActorClip(SpriteKeys.Clip(spriteSet, ClipNames.Idle), texture, ClipInfo.FromJson(json));
            catalog.SetClips(new List<ActorClip>(catalog.Clips) { idle });
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
            var texture = NewTexture(frames * FrameWidth, directions * FrameHeight);
            var json = Json(frames, directions, loop, hitFrame, FrameWidth, FrameHeight, 2, 1);
            return new ActorClip(SpriteKeys.Clip(spriteSet, name), texture, ClipInfo.FromJson(json));
        }

        private Texture2D NewTexture(int width, int height)
        {
            var texture = new Texture2D(width, height);
            _textures.Add(texture);
            return texture;
        }

        private static string Json(
            int frames, int directions, bool loop, int hitFrame, int width, int height, int pivotX, int pivotY) =>
            $"{{\"frames\":{frames},\"directions\":{directions},\"fps\":10,\"loop\":{(loop ? "true" : "false")}," +
            $"\"hitFrame\":{hitFrame},\"frameWidth\":{width},\"frameHeight\":{height},\"pivotX\":{pivotX},\"pivotY\":{pivotY}}}";
    }
}
