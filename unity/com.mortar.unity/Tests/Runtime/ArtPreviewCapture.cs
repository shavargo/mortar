#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class ArtPreviewCapture
    {
        public const int Width = 1280;
        public const int Height = 720;

        [UnityTest]
        [Explicit("Writes art preview images to out/; run with -Filter ArtPreviewCapture")]
        public IEnumerator CaptureTownAndCathedral()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>("Assets/Art/SpriteCatalog.asset");
            Assert.IsNotNull(catalog, "Rebuild the sprite catalog first");
            var folder = TestPaths.FreshSaveFolder();
            var host = new GameObject("art-preview");
            host.SetActive(false);
            var app = host.AddComponent<MortarApp>();
            app.Configure(catalog, UiRoot.CreatePanelSettings(null), folder);
            host.SetActive(true);

            app.StartNewGame();
            yield return null;
            Capture(app, "town");
            Journeys.TakeStairsDown(app.Game);
            yield return null;
            Capture(app, "cathedral");

            Object.Destroy(host);
            TestPaths.Delete(folder);
        }

        private static void Capture(MortarApp app, string name)
        {
            CaptureCamera(app.CameraRig.Camera, name);
            app.CameraRig.Filter.Toggle();
            CaptureCamera(app.CameraRig.Camera, name + "-raw");
            app.CameraRig.Filter.Toggle();
        }

        private static void CaptureCamera(Camera camera, string name)
        {
            var target = new RenderTexture(Width, Height, 24);
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = null;
            var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "out", $"art-preview-{name}.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
            target.Release();
            Object.Destroy(target);
            Debug.Log($"[Mortar] Wrote {path}");
        }
    }
}
#endif
