using UnityEngine;

namespace Mortar.Unity
{
    public sealed class AsciiResources
    {
        public const string ShaderPath = "Mortar/Ascii";
        public const string MaskShaderPath = "Mortar/AsciiMask";
        public const string AtlasPath = "Mortar/GlyphAtlas";

        public AsciiResources(Shader? shader, Shader? maskShader, Texture2D? atlas)
        {
            Shader = shader;
            MaskShader = maskShader;
            Atlas = atlas;
        }

        public Shader? Shader { get; }

        public Shader? MaskShader { get; }

        public Texture2D? Atlas { get; }

        public string? Problem =>
            Missing(Shader, ShaderPath)
            ?? Missing(MaskShader, MaskShaderPath)
            ?? (Atlas == null ? $"glyph atlas '{AtlasPath}' did not load" : null);

        public static AsciiResources Load() =>
            new AsciiResources(
                Resources.Load<Shader>(ShaderPath), Resources.Load<Shader>(MaskShaderPath), Resources.Load<Texture2D>(AtlasPath));

        private static string? Missing(Shader? shader, string path)
        {
            if (shader == null)
            {
                return $"shader '{path}' did not load";
            }
            return shader.isSupported ? null : $"shader '{shader.name}' is not supported on this GPU";
        }
    }
}
