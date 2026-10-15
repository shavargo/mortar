using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mortar.Unity.EditorTools
{
    public static class SpriteCatalogBuilder
    {
        public const string ArtRoot = "Assets/Art";
        public const string CatalogPath = "Assets/Art/SpriteCatalog.asset";

        [MenuItem("Mortar/Rebuild Sprite Catalog")]
        public static void RebuildFromMenu()
        {
            var catalog = Rebuild();
            Debug.Log($"Mortar: sprite catalog has {catalog.Count} sprites and {catalog.ClipCount} clips");
        }

        public static void RebuildFromCommandLine()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            RebuildFromMenu();
        }

        public static SpriteCatalog Rebuild()
        {
            EnsureFolder(ArtRoot);
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.SetEntries(FindSprites());
            catalog.SetClips(FindClips());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static List<SpriteEntry> FindSprites()
        {
            var entries = new List<SpriteEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var key = KeyFor(path, ArtRoot);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (key != null && sprite != null)
                {
                    entries.Add(new SpriteEntry(key, sprite));
                }
            }
            return entries;
        }

        private static List<ActorClip> FindClips()
        {
            var clips = new List<ActorClip>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { ArtRoot }))
            {
                var jsonPath = AssetDatabase.GUIDToAssetPath(guid);
                var sheetPath = Path.ChangeExtension(jsonPath, ".png").Replace('\\', '/');
                if (!jsonPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || !ArtImport.IsSheet(sheetPath))
                {
                    continue;
                }
                var key = KeyFor(sheetPath, ArtRoot);
                var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
                if (key != null && sheet != null && text != null)
                {
                    var info = ClipInfo.FromJson(text.text);
                    info.CheckSheet(sheet.width, sheet.height, sheetPath);
                    clips.Add(new ActorClip(key, sheet, info));
                }
            }
            return clips;
        }

        public static string? KeyFor(string assetPath, string root)
        {
            var normalized = assetPath.Replace('\\', '/');
            var prefix = root.TrimEnd('/') + "/";
            if (!normalized.StartsWith(prefix, StringComparison.Ordinal)
                || !normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return normalized.Substring(prefix.Length, normalized.Length - prefix.Length - ".png".Length);
        }

        public static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
