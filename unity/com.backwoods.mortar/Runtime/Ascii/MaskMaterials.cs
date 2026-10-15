using UnityEngine;

namespace Mortar.Unity
{
    public static class MaskMaterials
    {
        public const string StrokeShaderPath = "Mortar/StrokeSprite";

        private static Material? _stroke;

        public static Material? Stroke
        {
            get
            {
                if (_stroke == null)
                {
                    _stroke = Create(StrokeShaderPath);
                }
                return _stroke;
            }
        }

        public const string GlowShaderPath = "Mortar/GlowSprite";

        private static Material? _glow;

        public static Material? Glow
        {
            get
            {
                if (_glow == null)
                {
                    _glow = Create(GlowShaderPath);
                }
                return _glow;
            }
        }

        private static Material? Create(string path)
        {
            var shader = Resources.Load<Shader>(path);
            if (shader == null || !shader.isSupported)
            {
                return null;
            }
            return new Material(shader) { name = shader.name, hideFlags = HideFlags.DontSave };
        }
    }
}
