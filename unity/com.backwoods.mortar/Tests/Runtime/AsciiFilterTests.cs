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
            var broken = AsciiFilter.Attach(_scene.Camera, new AsciiResources(null, null, null));
            _scene.AddCells(0, 0, 100, 60, Color.white);

            _scene.Render();

            Assert.IsFalse(broken.IsUsable);
            StringAssert.Contains("did not load", broken.Problem);
            Assert.AreEqual(255, _scene.Pixel(300, 300).r);
        }

        [Test]
        public void StrokeSpritesMarkTheMaskUnlessSomethingCoversThem()
        {
            _scene.AddCells(0, 0, 100, 60, Color.white, MaskMaterials.Stroke);
            _scene.AddCells(20, 0, 20, 60, Color.white, null, 1);
            _scene.AddCells(100, 0, 100, 60, Color.white);

            _scene.Render();

            var mask = _filter.MaskTexture!;
            var pixels = _scene.Read(mask);
            Assert.AreEqual(new CellGrid(AsciiScene.Width, AsciiScene.Height).Columns, mask.width);
            Assert.AreEqual(255, pixels[30 * mask.width + 50].r, "the stroke sprite marks its cells");
            Assert.AreEqual(0, pixels[30 * mask.width + 30].r, "a sprite in front clears the mask");
            Assert.AreEqual(0, pixels[30 * mask.width + 150].r, "plain sprites never mark the mask");
        }

        [Test]
        public void StrokeCellsDrawThinDashesInsteadOfDenseGlyphs()
        {
            _scene.AddCells(0, 0, 100, 60, Color.white, MaskMaterials.Stroke);
            _scene.AddCells(100, 0, 100, 60, Color.white);

            _scene.Render();

            Assert.Greater(_scene.LitPixels(50, 30), 0);
            Assert.Less(_scene.LitPixels(50, 30) * 2, _scene.LitPixels(150, 30));
        }

        [Test]
        public void WaterSpritesMarkTheMaskHalfwayAndStayThinDashes()
        {
            _scene.AddCells(0, 0, 100, 60, Color.white, MaskMaterials.Water);
            _scene.AddCells(100, 0, 100, 60, Color.white);

            _scene.Render();

            var mask = _filter.MaskTexture!;
            var red = _scene.Read(mask)[30 * mask.width + 50].r;
            Assert.That(red, Is.InRange(100, 150), "water marks the mask at half red, not full grass red");
            Assert.Greater(_scene.LitPixels(50, 30), 0);
            Assert.Less(_scene.LitPixels(50, 30) * 2, _scene.LitPixels(150, 30));
        }

        [Test]
        public void InTheDarkOnlyTheLightRadiusShowsGlyphs()
        {
            _scene.AddCells(0, 0, 200, 60, Color.white);
            _filter.Ambient = 0f;
            _filter.Lights.Add(new AsciiLight(Vector3.zero, 2f, 1f));

            _scene.Render();

            Assert.Greater(_scene.LitPixels(100, 30), 0, "the lit centre should draw glyphs");
            Assert.AreEqual(0, _scene.LitPixels(100, 52), "the light is half as tall as it is wide");
            Assert.AreEqual(0, _scene.LitPixels(0, 0), "outside the light is blank");
        }

        [Test]
        public void FullAmbientLightsEverything()
        {
            _scene.AddCells(0, 0, 200, 60, Color.white);
            _filter.Ambient = 1f;

            _scene.Render();

            Assert.Greater(_scene.LitPixels(1, 1), 0);
        }

        [Test]
        public void BrightCellsBleedIntoTheirNeighbours()
        {
            _scene.AddCells(99, 29, 2, 2, Color.white);

            _scene.Render();

            Assert.Greater(_scene.LitPixels(101, 30), 0, "bloom should light the cell beside a bright one");
        }

        [Test]
        public void DimCellsDoNotBloom()
        {
            _scene.AddCells(99, 29, 2, 2, new Color(0.5f, 0.5f, 0.5f));

            _scene.Render();

            Assert.AreEqual(0, _scene.LitPixels(101, 30));
        }

        [Test]
        public void GlowingSpritesBloomEvenWhenDim()
        {
            _scene.AddCells(99, 29, 2, 2, new Color(0.4f, 0.4f, 0.4f), MaskMaterials.Glow);

            _scene.Render();

            Assert.AreEqual(255, _scene.Read(_filter.MaskTexture!)[30 * 200 + 99].g, "glow sprites mark the mask green");
            Assert.Greater(_scene.LitPixels(101, 30), 0, "glow should light the neighbouring cell");
        }

        [Test]
        public void FigureSpritesStayReadableWhenDimUnderTheLight()
        {
            _scene.AddCells(90, 25, 20, 10, new Color(0.12f, 0.12f, 0.12f), MaskMaterials.Figure);
            _filter.Ambient = 0f;
            _filter.Lights.Add(new AsciiLight(Vector3.zero, 3f, 1f));

            _scene.Render();

            Assert.AreEqual(255, _scene.Read(_filter.MaskTexture!)[30 * 200 + 100].b, "figures mark the mask blue");
            Assert.Greater(_scene.LitPixels(100, 30), 0, "dark figures keep glyphs inside the light");
        }

        [Test]
        public void LivingWalkersStampTheTrampleField()
        {
            _scene.AddCells(0, 0, 200, 60, Color.white, MaskMaterials.Stroke);
            _filter.Tramplers.Add(Vector3.zero);
            _filter.TrampleDeltaSeconds = 0f;

            _scene.Render();

            var field = _filter.TrampleTexture!;
            var pixels = _scene.Read(field);
            var centre = pixels[(field.height / 2) * field.width + field.width / 2];
            var corner = pixels[0];
            Assert.Greater(centre.r, 200, "a walker at the origin stamps the centre cells");
            Assert.Less(corner.r, 40, "cells far from the walker stay untrampled");
        }

        [Test]
        public void TrampleFlattensGrassStrokesTowardThinDashes()
        {
            _scene.AddCells(0, 0, 200, 60, Color.white, MaskMaterials.Stroke);
            _filter.TrampleDeltaSeconds = 0f;
            _scene.Render();
            var windy = _scene.LitPixels(100, 30);

            _filter.ClearTrample();
            _filter.Tramplers.Add(Vector3.zero);
            _filter.TrampleDeltaSeconds = 0f;
            _scene.Render();
            var flattened = _scene.LitPixels(100, 30);

            Assert.Greater(windy, 0);
            Assert.Greater(flattened, 0);
            Assert.LessOrEqual(flattened, windy, "flat dashes should not ink denser than windy lean");
        }

        [Test]
        public void TrampleDecaysAfterWalkersLeave()
        {
            _scene.AddCells(0, 0, 200, 60, Color.white, MaskMaterials.Stroke);
            _filter.Tramplers.Add(Vector3.zero);
            _filter.TrampleDeltaSeconds = 0f;
            _scene.Render();
            var stamped = _scene.Read(_filter.TrampleTexture!)[
                (_filter.TrampleTexture!.height / 2) * _filter.TrampleTexture.width + _filter.TrampleTexture.width / 2].r;

            _filter.Tramplers.Clear();
            _filter.TrampleDeltaSeconds = AsciiSettings.TrampleHalfLife * 4f;
            _scene.Render();
            var faded = _scene.Read(_filter.TrampleTexture!)[
                (_filter.TrampleTexture!.height / 2) * _filter.TrampleTexture.width + _filter.TrampleTexture.width / 2].r;

            Assert.Greater(stamped, 200);
            Assert.Less(faded, stamped / 4, "four half-lives should leave little trail");
        }
    }
}
