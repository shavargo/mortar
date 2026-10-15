using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mortar.Unity.EditorTools
{
    public sealed class ArtImport : AssetPostprocessor
    {
        public const int MaxSheetSize = 8192;

        private void OnPreprocessTexture()
        {
            if (SpriteCatalogBuilder.KeyFor(assetPath, SpriteCatalogBuilder.ArtRoot) == null)
            {
                return;
            }
            var importer = (TextureImporter)assetImporter;
            if (IsSheet(assetPath))
            {
                ConfigureSheet(importer);
                return;
            }
            importer.GetSourceTextureWidthAndHeight(out var width, out var height);
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = PivotFor(assetPath, width, height);
            settings.spritePixelsPerUnit = GeneratedSprites.PixelsPerUnit;
            importer.SetTextureSettings(settings);
        }

        public static bool IsSheet(string assetPath) => IsSheet(assetPath, File.Exists);

        public static bool IsSheet(string assetPath, Func<string, bool> fileExists)
        {
            var key = SpriteCatalogBuilder.KeyFor(assetPath, SpriteCatalogBuilder.ArtRoot);
            return key != null && key.StartsWith("actors/", StringComparison.Ordinal)
                && fileExists(Path.ChangeExtension(assetPath, ".json"));
        }

        public static Vector2 PivotFor(string assetPath, int width, int height)
        {
            var key = SpriteCatalogBuilder.KeyFor(assetPath, SpriteCatalogBuilder.ArtRoot) ?? "";
            if (height <= 0)
            {
                return new Vector2(0.5f, 0.5f);
            }
            if (key.StartsWith("tiles/", StringComparison.Ordinal))
            {
                return new Vector2(0.5f, Mathf.Min(0.5f, 16f / height));
            }
            if (key.StartsWith("actors/", StringComparison.Ordinal))
            {
                return new Vector2(0.5f, 4f / height);
            }
            return new Vector2(0.5f, 0.5f);
        }

        private static void ConfigureSheet(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = MaxSheetSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
