using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Mortar.Unity.Tests
{
    public sealed class AsciiFilterTests
    {
        private AsciiScene _scene = null!;
        private AsciiFilter _filter = null!;

        [SetUp]
        public void SetUp()
        {
            _scene = new AsciiScene();
            _filter = AsciiFilter.Attach(_scene.Camera, AsciiResources.Load());
        }

        [TearDown]
        public void TearDown() => _scene.Dispose();

        private static int BackgroundLevel => Mathf.RoundToInt(AsciiSettings.Background * 255f);

        [Test]
        public void TheFilterLoadsItsResources()
        {
            Assert.IsNull(_filter.Problem);
            Assert.IsTrue(_filter.IsUsable);
        }

        [Test]
        public void ABlackSceneShowsOnlyTheBackground()
        {
            _scene.Render();

            for (var y = 0; y < AsciiScene.Height; y += 7)
            {
                for (var x = 0; x < AsciiScene.Width; x += 5)
                {
                    Assert.That(_scene.Pixel(x, y).r, Is.InRange(BackgroundLevel - 1, BackgroundLevel + 1), $"pixel {x},{y}");
                }
            }
        }

        [Test]
        public void ABrightAreaShowsGlyphsInItsColour()
        {
            _scene.AddCells(80, 20, 40, 20, new Color(1f, 0.5f, 0.2f));

            _scene.Render();

            var brightest = new Color32(0, 0, 0, 255);
            var background = 0;
            for (var y = 300; y < 310; y++)
            {
                for (var x = 600; x < 606; x++)
                {
                    var pixel = _scene.Pixel(x, y);
                    if (pixel.r > brightest.r) brightest = pixel;
                    if (pixel.r <= BackgroundLevel + 1) background++;
                }
            }
            Assert.Greater(brightest.r, 150, "the glyph should be inked in the area's colour");
            Assert.Greater(brightest.r, brightest.g);
            Assert.Greater(brightest.g, brightest.b);
            Assert.Greater(background, 0, "a glyph is not a solid block");
        }

        [Test]
        public void AVerticalBoundaryShowsABarGlyph()
        {
            _scene.AddCells(0, 0, 100, 60, Color.white);

            _scene.Render();

            var edge = _scene.InkSpan(99, 30);
            var inside = _scene.InkSpan(98, 30);
            Assert.LessOrEqual(edge.x, 2, "the boundary cell should be a thin vertical stroke");
            Assert.GreaterOrEqual(edge.y, 6);
            Assert.Greater(inside.x, 2, "a bright interior cell should use a dense ramp glyph");
        }

        [Test]
        public void TogglingShowsTheRawScene()
        {
            _scene.AddCells(0, 0, 100, 60, Color.white);
            _filter.Toggle();

            _scene.Render();

            Assert.IsFalse(_filter.IsOn);
            Assert.AreEqual(255, _scene.Pixel(300, 300).r);
            Assert.AreEqual(0, _scene.Pixel(900, 300).r);
        }

        [Test]
        public void AMissingShaderShowsTheRawSceneAndSaysWhy()
        {
            LogAssert.Expect(LogType.Warning, new Regex("ASCII filter off.*did not load"));
            Object.DestroyImmediate(_filter);
            var broken = AsciiFilter.Attach(_scene.Camera, new AsciiResources(null, null));
            _scene.AddCells(0, 0, 100, 60, Color.white);

            _scene.Render();

            Assert.IsFalse(broken.IsUsable);
            StringAssert.Contains("did not load", broken.Problem);
            Assert.AreEqual(255, _scene.Pixel(300, 300).r);
        }
    }
}
