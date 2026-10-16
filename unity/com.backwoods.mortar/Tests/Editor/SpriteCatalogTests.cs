using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class SpriteCatalogTests
    {
        private SpriteCatalog _catalog = null!;

        [SetUp]
        public void SetUp() => _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_catalog);

        [Test]
        public void ReturnsTheMappedTileVariant()
        {
            var sprite = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("tiles/cathedral/floor-1", sprite) });

            Assert.AreSame(sprite, _catalog.Tile("cathedral", "floor", 1, false));
        }

        [Test]
        public void MissingVariantFallsBackToVariantZero()
        {
            var sprite = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("tiles/cathedral/floor-0", sprite) });

            Assert.AreSame(sprite, _catalog.Tile("cathedral", "floor", 2, false));
        }

        [Test]
        public void MissingTileUsesThePlaceholderForItsShape()
        {
            LogAssert.Expect(LogType.Warning, new Regex("tiles/town/wall-0"));
            LogAssert.Expect(LogType.Warning, new Regex("tiles/town/grass-0"));

            Assert.AreSame(GeneratedSprites.Raised, _catalog.Tile("town", "wall", 0, true));
            Assert.AreSame(GeneratedSprites.Flat, _catalog.Tile("town", "grass", 0, false));
        }

        [Test]
        public void WarnsOncePerMissingKey()
        {
            LogAssert.Expect(LogType.Warning, new Regex("actors/zombie"));

            _catalog.Actor("zombie");
            _catalog.Actor("zombie");

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ActorsAndIconsResolveByKey()
        {
            var actor = MakeSprite();
            var icon = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("actors/warrior", actor), new SpriteEntry("icons/iron-knife", icon) });

            Assert.AreSame(actor, _catalog.Actor("warrior"));
            Assert.AreSame(icon, _catalog.Icon("iron-knife"));
            Assert.IsTrue(_catalog.Has("icons/iron-knife"));
            Assert.AreEqual(2, _catalog.Count);
        }

        [Test]
        public void MissingIconUsesTheIconPlaceholder()
        {
            LogAssert.Expect(LogType.Warning, new Regex("icons/gold"));

            Assert.AreSame(GeneratedSprites.Icon, _catalog.Icon("gold"));
        }

        [Test]
        public void ClipsResolveBySpriteSetAndName()
        {
            var idle = new ActorClip("actors/warrior/idle", null, TestClipInfo.Make(2, 8, loop: true));
            _catalog.SetClips(new[] { idle });

            Assert.AreSame(idle, _catalog.Clip("warrior", ClipNames.Idle));
            Assert.IsNull(_catalog.Clip("warrior", ClipNames.Walk));
            Assert.IsNull(_catalog.Clip("zombie", ClipNames.Idle));
            Assert.AreEqual(1, _catalog.ClipCount);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void IconsPreferTheGlyphVersion()
        {
            var painted = MakeSprite();
            var glyph = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("icons/iron-knife", painted), new SpriteEntry("glyph-icons/iron-knife", glyph) });

            Assert.AreSame(glyph, _catalog.Icon("iron-knife"));
        }

        [Test]
        public void WorldIconsKeepThePaintedIcon()
        {
            var painted = MakeSprite();
            var glyph = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("icons/iron-knife", painted), new SpriteEntry("glyph-icons/iron-knife", glyph) });

            Assert.AreSame(painted, _catalog.WorldIcon("iron-knife"));
        }

        [Test]
        public void AMissingGlyphIconFallsBackToThePaintedIconWithoutWarning()
        {
            var painted = MakeSprite();
            _catalog.SetEntries(new[] { new SpriteEntry("icons/iron-knife", painted) });

            Assert.AreSame(painted, _catalog.Icon("iron-knife"));
            LogAssert.NoUnexpectedReceived();
        }

        private static Sprite MakeSprite()
        {
            var texture = new Texture2D(4, 4);
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), GeneratedSprites.PixelsPerUnit);
        }
    }
}
