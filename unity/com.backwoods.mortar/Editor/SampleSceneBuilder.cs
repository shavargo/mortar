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

        [MenuItem("Mortar/Build Sample Scene")]
        public static void Build()
        {
            var catalog = SpriteCatalogBuilder.Rebuild();
            var settings = PanelSettingsAsset();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var app = new GameObject("Mortar").AddComponent<MortarApp>();
            var serialized = new SerializedObject(app);
            serialized.FindProperty("_catalog").objectReferenceValue = catalog;
            serialized.FindProperty("_panelSettings").objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SpriteCatalogBuilder.EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
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
    }
}
