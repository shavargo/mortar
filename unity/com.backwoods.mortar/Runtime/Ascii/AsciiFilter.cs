using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mortar.Unity
{
    [RequireComponent(typeof(Camera))]
    public sealed class AsciiFilter : MonoBehaviour
    {
        private const int DownsamplePass = 0;
        private const int GlyphPass = 1;

        private static readonly int CellCountId = Shader.PropertyToID("_CellCount");
        private static readonly int CellTexId = Shader.PropertyToID("_CellTex");

        private readonly List<Object> _owned = new List<Object>();
        private InputAction _toggle = null!;
        private Material? _material;

        public bool IsUsable => _material != null;

        public bool IsOn { get; private set; } = true;

        public string? Problem { get; private set; }

        public static AsciiFilter Attach(Camera camera, AsciiResources resources)
        {
            var filter = camera.gameObject.AddComponent<AsciiFilter>();
            filter.Use(resources);
            return filter;
        }

        public void Toggle() => IsOn = !IsOn;

        private void Use(AsciiResources resources)
        {
            Problem = resources.Problem;
            if (Problem != null)
            {
                Debug.LogWarning($"Mortar: ASCII filter off, showing the raw scene: {Problem}");
                return;
            }
            _material = AsciiMaterial.Create(resources.Shader!, resources.Atlas!, _owned);
        }

        private void Awake()
        {
            _toggle = new InputAction("ascii-toggle", InputActionType.Button, "<Keyboard>/f9");
            _toggle.performed += _ => Toggle();
        }

        private void OnEnable() => _toggle.Enable();

        private void OnDisable() => _toggle.Disable();

        private void OnDestroy()
        {
            _toggle.Dispose();
            foreach (var item in _owned)
            {
                if (item != null) Destroy(item);
            }
            _owned.Clear();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null || !IsOn)
            {
                Graphics.Blit(source, destination);
                return;
            }
            var grid = new CellGrid(source.width, source.height);
            _material.SetVector(CellCountId, new Vector4(grid.Columns, grid.Rows, 0f, 0f));
            var cells = CellTexture(grid);
            Graphics.Blit(source, cells, _material, DownsamplePass);
            _material.SetTexture(CellTexId, cells);
            Graphics.Blit(source, destination, _material, GlyphPass);
            RenderTexture.ReleaseTemporary(cells);
        }

        private static RenderTexture CellTexture(CellGrid grid)
        {
            var texture = RenderTexture.GetTemporary(grid.Columns, grid.Rows, 0, RenderTextureFormat.ARGB32);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
