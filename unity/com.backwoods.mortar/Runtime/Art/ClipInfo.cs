using System;
using UnityEngine;

namespace Mortar.Unity
{
    [Serializable]
    public sealed class ClipInfo
    {
        // Field names match the JSON keys written by tools/art/mortar_art/sheets.py.
        [SerializeField] private int frames = 0;
        [SerializeField] private int directions = 0;
        [SerializeField] private float fps = 0f;
        [SerializeField] private bool loop = false;
        [SerializeField] private int hitFrame = 0;
        [SerializeField] private int frameWidth = 0;
        [SerializeField] private int frameHeight = 0;
        [SerializeField] private int pivotX = 0;
        [SerializeField] private int pivotY = 0;

        public int Frames => frames;

        public int Directions => directions;

        public float Fps => fps;

        public bool Loop => loop;

        public int HitFrame => hitFrame;

        public int FrameWidth => frameWidth;

        public int FrameHeight => frameHeight;

        public int PivotX => pivotX;

        public int PivotY => pivotY;

        public float Seconds => fps > 0f ? frames / fps : 0f;

        public static ClipInfo FromJson(string json)
        {
            var info = JsonUtility.FromJson<ClipInfo>(json);
            if (info == null || info.frames < 1 || (info.directions != 1 && info.directions != 8)
                || info.fps <= 0f || info.frameWidth < 1 || info.frameHeight < 1)
            {
                throw new FormatException("sprite sheet metadata needs frames >= 1, 1 or 8 directions, fps > 0 and a frame size");
            }
            return info;
        }
    }
}
