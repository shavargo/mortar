using UnityEngine;
using UnityEngine.UIElements;

namespace Mortar.Unity
{
    public sealed class UiRoot : MonoBehaviour
    {
        private UIDocument _document = null!;

        public VisualElement Root => _document.rootVisualElement;

        public VisualElement Overlay { get; private set; } = null!;

        public VisualElement Hud { get; private set; } = null!;

        public VisualElement Screens { get; private set; } = null!;

        public VisualElement Modal { get; private set; } = null!;

        public static UiRoot Create(Transform parent, PanelSettings settings)
        {
            var go = new GameObject("UI");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var document = go.AddComponent<UIDocument>();
            document.panelSettings = settings;
            var ui = go.AddComponent<UiRoot>();
            ui._document = document;
            go.SetActive(true);
            ui.BuildLayers();
            return ui;
        }

        public static PanelSettings CreatePanelSettings(ThemeStyleSheet? theme)
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.themeStyleSheet = theme;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1280, 720);
            settings.match = 0.5f;
            return settings;
        }

        public bool IsPointerOverUi(Vector2 screenPosition)
        {
            var panel = Root.panel;
            if (panel == null)
            {
                return false;
            }
            var point = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return panel.Pick(point) != null;
        }

        private void BuildLayers()
        {
            Root.pickingMode = PickingMode.Ignore;
            Overlay = Layer("overlay");
            Hud = Layer("hud");
            Screens = Layer("screens");
            Modal = Layer("modal");
        }

        private VisualElement Layer(string name)
        {
            var layer = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            Ui.Fill(layer);
            Root.Add(layer);
            return layer;
        }
    }
}
