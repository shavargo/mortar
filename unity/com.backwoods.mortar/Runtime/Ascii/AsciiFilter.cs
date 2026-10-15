using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mortar.Unity
{
    [RequireComponent(typeof(Camera))]
    public sealed class AsciiFilter : MonoBehaviour
    {
        public const int MaxLights = 4;

        private const int DownsamplePass = 0;
        private const int GlyphPass = 1;

        private static readonly int CellCountId = Shader.PropertyToID("_CellCount");
        private static readonly int CellTexId = Shader.PropertyToID("_CellTex");
        private static readonly int AmbientId = Shader.PropertyToID("_Ambient");
        private static readonly int LightCountId = Shader.PropertyToID("_LightCount");
        private static readonly int LightsId = Shader.PropertyToID("_Lights");

        private readonly List<Object> _owned = new List<Object>();
        private readonly Vector4[] _lightData = new Vector4[MaxLights];
        private InputAction _toggle = null!;
        private Camera _camera = null!;
        private Material? _material;

        public bool IsUsable => _material != null;

        public bool IsOn { get; private set; } = true;

        public string? Problem { get; private set; }

        public float Ambient { get; set; } = 1f;

        public List<AsciiLight> Lights { get; } = new List<AsciiLight>();

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
            _camera = GetComponent<Camera>();
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
            ApplyLights(_material, source.width, source.height);
            var cells = CellTexture(grid);
            Graphics.Blit(source, cells, _material, DownsamplePass);
            _material.SetTexture(CellTexId, cells);
            Graphics.Blit(source, destination, _material, GlyphPass);
            RenderTexture.ReleaseTemporary(cells);
        }

        private void ApplyLights(Material material, int width, int height)
        {
            var count = Mathf.Min(Lights.Count, MaxLights);
            var pixelsPerUnit = height / (2f * _camera.orthographicSize);
            for (var i = 0; i < count; i++)
            {
                var light = Lights[i];
                var viewport = _camera.WorldToViewportPoint(light.Position);
                _lightData[i] = new Vector4(viewport.x * width, viewport.y * height, light.Radius * pixelsPerUnit, light.Intensity);
            }
            material.SetFloat(AmbientId, Ambient);
            material.SetFloat(LightCountId, count);
            material.SetVectorArray(LightsId, _lightData);
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
