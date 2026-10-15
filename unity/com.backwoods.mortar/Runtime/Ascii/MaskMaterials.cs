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
