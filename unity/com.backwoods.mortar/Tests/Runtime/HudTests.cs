using System.Collections;
using Mortar.Game.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Mortar.Unity.Tests
{
    public sealed class HudTests
    {
        private GameObject _root = null!;
        private SpriteCatalog _catalog = null!;
        private UiRoot _ui = null!;
        private HudView _hud = null!;
        private World _world = null!;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("hud-test");
            _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();
            _ui = UiRoot.Create(_root.transform, UiRoot.CreatePanelSettings(null));
            _hud = new HudView(_ui, _catalog);
            _world = TestContent.NewSession().World;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_catalog);
        }

        [Test]
        public void OrbsShowLifeAndMana()
        {
            _hud.Refresh(_world, new string[0], null);

            var player = _world.Player;
            Assert.AreEqual($"{player.Life}/{player.Profile.MaxLife}", _hud.Life.Text);
            Assert.AreEqual($"{player.Mana}/{player.Profile.MaxMana}", _hud.Mana.Text);
        }

        [Test]
        public void BeltShowsTheStartingPotions()
        {
            _hud.Refresh(_world, new string[0], null);

            Assert.AreEqual(2, _hud.Belt.FilledSlots);
        }

        [Test]
        public void MessagesAndHoverTextAreShown()
        {
            _hud.Refresh(_world, new[] { "Entered depth 1" }, "Healing Draught");

            CollectionAssert.AreEqual(new[] { "Entered depth 1" }, _hud.MessageTexts);
            Assert.AreEqual("Healing Draught", _hud.HoverTextShown);
        }

        [Test]
        public void FloatingTextsExpire()
        {
            var texts = new FloatingTexts(_ui.Overlay);
            texts.Spawn("12", Vector3.zero, Color.white, 0f);

            texts.Update(null, FloatingTexts.Lifetime + 0.1f);

            Assert.AreEqual(0, texts.Count);
        }

        [Test]
        public void TheInterfaceUsesTheMonospaceFont()
        {
            Assert.IsNotNull(_ui.Root.style.unityFontDefinition.value.font);
        }

        [Test]
        public void FloatingTextsAreNotBold()
        {
            var texts = new FloatingTexts(_ui.Overlay);
            texts.Spawn("12", Vector3.zero, Color.white, 0f);

            Assert.AreNotEqual(FontStyle.Bold, _ui.Overlay.Q<Label>().style.unityFontStyleAndWeight.value);
        }

        [UnityTest]
        public IEnumerator EmptyScreenCenterIsNotUi()
        {
            _hud.Refresh(_world, new string[0], null);
            yield return null;

            Assert.IsFalse(_ui.IsPointerOverUi(new Vector2(Screen.width / 2f, Screen.height / 2f)));
        }

        [UnityTest]
        public IEnumerator BeltSlotCentreIsUi()
        {
            _hud.Refresh(_world, new string[0], null);
            yield return null;

            var panelSize = RuntimePanelUtils.ScreenToPanel(_ui.Root.panel, new Vector2(Screen.width, Screen.height));
            var scale = new Vector2(Screen.width / panelSize.x, Screen.height / panelSize.y);
            var centre = Vector2.Scale(_ui.Root.Q("belt-1").worldBound.center, scale);

            Assert.IsTrue(_ui.IsPointerOverUi(new Vector2(centre.x, Screen.height - centre.y)));
        }
    }
}
