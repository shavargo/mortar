using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity
{
    public sealed class AsciiTrample : IDisposable
    {
        public const int MaxWalkers = 32;
        private const int DecayPass = 0;
        private const int StampPass = 1;

        private static readonly int DecayId = Shader.PropertyToID("_Decay");
        private static readonly int WalkerCountId = Shader.PropertyToID("_WalkerCount");
        private static readonly int WalkersId = Shader.PropertyToID("_Walkers");

        private readonly Vector4[] _walkers = new Vector4[MaxWalkers];
        private readonly Material _material;
        private RenderTexture? _field;
        private RenderTexture? _temp;

        public AsciiTrample(Shader shader)
        {
            _material = new Material(shader) { name = "Mortar Ascii Trample", hideFlags = HideFlags.DontSave };
        }

        public RenderTexture? Texture => _field;

        public void Advance(Camera camera, CellGrid grid, IReadOnlyList<Vector3> positions, float deltaTime)
        {
            Ensure(grid);
            var decay = Mathf.Exp(-Mathf.Max(deltaTime, 0f) * Mathf.Log(2f) / AsciiSettings.TrampleHalfLife);
            _material.SetFloat(DecayId, decay);
            Graphics.Blit(_field, _temp, _material, DecayPass);
            var count = FillWalkers(camera, grid, positions);
            _material.SetFloat(WalkerCountId, count);
            _material.SetVectorArray(WalkersId, _walkers);
            Graphics.Blit(_temp, _field, _material, StampPass);
        }

        public void Clear()
        {
            if (_field == null)
            {
                return;
            }
            var prior = RenderTexture.active;
            RenderTexture.active = _field;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prior;
        }

        public void Dispose()
        {
            Release();
            if (_material != null)
            {
                Object.Destroy(_material);
            }
        }

        private int FillWalkers(Camera camera, CellGrid grid, IReadOnlyList<Vector3> positions)
        {
            var height = camera.targetTexture != null ? camera.targetTexture.height
                : (camera.pixelHeight > 0 ? camera.pixelHeight : (int)(camera.orthographicSize * 2f));
            var pixelsPerUnit = height / (2f * camera.orthographicSize);
            var radiusPixels = AsciiSettings.TrampleRadiusTiles / Mathf.Sqrt(2f) * pixelsPerUnit;
            var radiusCells = new Vector2(
                Mathf.Max(1.25f, radiusPixels / AsciiSettings.CellWidth),
                Mathf.Max(1.25f, radiusPixels / AsciiSettings.CellHeight));
            var count = Mathf.Min(positions.Count, MaxWalkers);
            for (var i = 0; i < count; i++)
            {
                var viewport = camera.WorldToViewportPoint(positions[i]);
                var cellX = Mathf.Floor(viewport.x * grid.Columns) + 0.5f;
                var cellY = Mathf.Floor(viewport.y * grid.Rows) + 0.5f;
                _walkers[i] = new Vector4(cellX, cellY, radiusCells.x, radiusCells.y);
            }
            for (var i = count; i < MaxWalkers; i++)
            {
                _walkers[i] = Vector4.zero;
            }
            return count;
        }

        private void Ensure(CellGrid grid)
        {
            if (_field != null && _field.width == grid.Columns && _field.height == grid.Rows)
            {
                return;
            }
            Release();
            _field = Make(grid.Columns, grid.Rows, "Mortar Ascii Trample");
            _temp = Make(grid.Columns, grid.Rows, "Mortar Ascii Trample Temp");
        }

        private static RenderTexture Make(int width, int height, string name) =>
            new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

        private void Release()
        {
            if (_field != null)
            {
                _field.Release();
                Object.Destroy(_field);
                _field = null;
            }
            if (_temp != null)
            {
                _temp.Release();
                Object.Destroy(_temp);
                _temp = null;
            }
        }
    }
}
