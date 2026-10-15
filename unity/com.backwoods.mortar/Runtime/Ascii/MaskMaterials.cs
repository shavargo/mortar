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

        public const string FigureShaderPath = "Mortar/FigureSprite";

        private static Material? _figure;

        public static Material? Figure
        {
            get
            {
                if (_figure == null)
                {
                    _figure = Create(FigureShaderPath, AsciiSettings.FigureBoost);
                }
                return _figure;
            }
        }

        private static Material? Create(string path, float boost = 0f)
        {
            var shader = Resources.Load<Shader>(path);
            if (shader == null || !shader.isSupported)
            {
                return null;
            }
            var material = new Material(shader) { name = shader.name, hideFlags = HideFlags.DontSave };
            if (boost > 0f)
            {
                material.SetFloat("_Boost", boost);
            }
            return material;
        }
    }
}
