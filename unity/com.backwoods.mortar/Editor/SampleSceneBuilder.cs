using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity.EditorTools
{
    public static class SampleSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Game.unity";
        public const string PanelSettingsPath = "Assets/Settings/MortarPanelSettings.asset";
        public const string ThemePath = "Packages/com.backwoods.mortar/Runtime/UI/MortarTheme.tss";
        public const string DefaultThemeFolder = "Assets/UI Toolkit";
        public const string DefaultThemeSubfolder = DefaultThemeFolder + "/UnityThemes";
        public const string DefaultThemePath = DefaultThemeSubfolder + "/UnityDefaultRuntimeTheme.tss";

        [MenuItem("Mortar/Build Sample Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SpriteCatalogBuilder.Rebuild();
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>(SpriteCatalogBuilder.CatalogPath);
            var settings = PanelSettingsAsset();
            if (catalog == null || settings == null)
            {
                throw new InvalidOperationException($"[Mortar] Sample scene needs {SpriteCatalogBuilder.CatalogPath} and {PanelSettingsPath}");
            }
            var app = new GameObject("Mortar").AddComponent<MortarApp>();
            var serialized = new SerializedObject(app);
            serialized.FindProperty("_catalog").objectReferenceValue = catalog;
            serialized.FindProperty("_panelSettings").objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SpriteCatalogBuilder.EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            DeleteDefaultTheme();
            Debug.Log($"[Mortar] Sample scene written to {ScenePath}");
        }

        private static PanelSettings PanelSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                SpriteCatalogBuilder.EnsureFolder("Assets/Settings");
                settings = UiRoot.CreatePanelSettings(null);
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            }
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static void DeleteDefaultTheme()
        {
            AssetDatabase.DeleteAsset(DefaultThemePath);
            DeleteIfEmpty(DefaultThemeSubfolder);
            DeleteIfEmpty(DefaultThemeFolder);
        }

        private static void DeleteIfEmpty(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder) && AssetDatabase.FindAssets("", new[] { folder }).Length == 0)
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
