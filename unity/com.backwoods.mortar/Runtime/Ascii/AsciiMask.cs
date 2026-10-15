using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity
{
    public sealed class AsciiMask : IDisposable
    {
        private readonly Camera _camera;
        private readonly Shader _shader;

        public AsciiMask(Camera main, Shader shader)
        {
            _shader = shader;
            var go = new GameObject("Ascii Mask Camera");
            go.transform.SetParent(main.transform, false);
            _camera = go.AddComponent<Camera>();
            _camera.enabled = false;
        }

        public RenderTexture? Texture { get; private set; }

        public RenderTexture Render(Camera main, CellGrid grid)
        {
            if (Texture == null || Texture.width != grid.Columns || Texture.height != grid.Rows)
            {
                Release();
                Texture = new RenderTexture(grid.Columns, grid.Rows, 0, RenderTextureFormat.ARGB32)
                {
                    name = "Mortar Ascii Mask",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }
            _camera.CopyFrom(main);
            _camera.targetTexture = Texture;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.clear;
            _camera.allowMSAA = false;
            _camera.allowHDR = false;
            _camera.RenderWithShader(_shader, "RenderType");
            return Texture;
        }

        public void Dispose()
        {
            Release();
            if (_camera != null)
            {
                Object.Destroy(_camera.gameObject);
            }
        }

        private void Release()
        {
            if (Texture == null)
            {
                return;
            }
            if (_camera != null)
            {
                _camera.targetTexture = null;
            }
            Texture.Release();
            Object.Destroy(Texture);
            Texture = null;
        }
    }
}
