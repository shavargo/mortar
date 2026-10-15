using System;
using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    [Serializable]
    public sealed class ActorClip
    {
        public const float PixelsPerUnit = 128f;

        [SerializeField] private string _key = "";
        [SerializeField] private Texture2D? _sheet;
        [SerializeField] private ClipInfo _info = new ClipInfo();

        [NonSerialized] private Sprite?[]? _sprites;

        public ActorClip(string key, Texture2D? sheet, ClipInfo info)
        {
            _key = key;
            _sheet = sheet;
            _info = info;
        }

        public string Key => _key;

        public int Frames => _info.Frames;

        public int Directions => _info.Directions;

        public float Fps => _info.Fps;

        public bool Loop => _info.Loop;

        public int HitFrame => _info.HitFrame;

        public float Seconds => _info.Seconds;

        public Sprite Frame(Direction direction, int index)
        {
            if (_sheet == null)
            {
                return GeneratedSprites.Actor;
            }
            var row = _info.Directions == 1 ? 0 : (int)direction;
            var column = Mathf.Clamp(index, 0, _info.Frames - 1);
            if (_sprites == null)
            {
                _sprites = new Sprite?[_info.Directions * _info.Frames];
            }
            var slot = row * _info.Frames + column;
            var sprite = _sprites[slot];
            if (sprite == null)
            {
                sprite = CreateSprite(_sheet, row, column);
                _sprites[slot] = sprite;
            }
            return sprite;
        }

        private Sprite CreateSprite(Texture2D sheet, int row, int column)
        {
            var width = _info.FrameWidth;
            var height = _info.FrameHeight;
            var rect = new Rect(column * width, sheet.height - (row + 1) * height, width, height);
            var pivot = new Vector2((float)_info.PivotX / width, (float)_info.PivotY / height);
            var sprite = Sprite.Create(sheet, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.Tight);
            sprite.name = $"{_key}/{row}/{column}";
            return sprite;
        }
    }
}
