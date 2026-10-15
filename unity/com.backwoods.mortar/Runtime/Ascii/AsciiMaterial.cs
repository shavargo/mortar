using System.Collections.Generic;
using UnityEngine;

namespace Mortar.Unity
{
    public static class AsciiMaterial
    {
        public static Material Create(Shader shader, Texture2D atlas, List<Object> owned)
        {
            var material = new Material(shader) { name = "Mortar Ascii", hideFlags = HideFlags.DontSave };
            owned.Add(material);
            material.SetTexture("_GlyphAtlas", atlas);
            material.SetTexture("_RampLut", Own(GlyphLookups.Ramp(AsciiSettings.DarkThreshold), owned));
            material.SetTexture("_EdgeLut", Own(GlyphLookups.Edges(AsciiSettings.EdgeThreshold), owned));
            material.SetTexture("_FalloffLut", Own(GlyphLookups.Falloff(), owned));
            material.SetFloat("_Background", AsciiSettings.Background);
            material.SetFloat("_Normalize", AsciiSettings.Normalize);
            material.SetFloat("_Saturation", AsciiSettings.Saturation);
            material.SetFloat("_DarkThreshold", AsciiSettings.DarkThreshold);
            material.SetFloat("_StrokeScale", AsciiSettings.StrokeScale);
            material.SetFloat("_StrokeSpeed", AsciiSettings.StrokeSpeed);
            material.SetFloat("_BloomThreshold", AsciiSettings.BloomThreshold);
            material.SetFloat("_BloomStrength", AsciiSettings.BloomStrength);
            material.SetFloat("_GlowBoost", AsciiSettings.GlowBoost);
            return material;
        }

        private static Texture2D Own(Texture2D texture, List<Object> owned)
        {
            owned.Add(texture);
            return texture;
        }
    }
}
