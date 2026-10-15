using System;
using System.Collections.Generic;
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
            Debug.Log($"Mortar: sprite catalog has {catalog.Count} entries");
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
            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
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
