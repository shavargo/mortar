using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class AsciiTrampleTests
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

        [Test]
        public void TrampleStaysOnTheWorldWhenTheCameraMoves()
        {
            _scene.AddCells(0, 0, 200, 60, Color.white, MaskMaterials.Stroke);
            var worldSpot = Vector3.zero;
            _filter.Tramplers.Add(worldSpot);
            _filter.TrampleDeltaSeconds = 0f;
            _scene.Render();

            _filter.Tramplers.Clear();
            _filter.TrampleDeltaSeconds = 0f;
            var shiftCells = 20;
            _scene.Camera.transform.position += new Vector3(
                shiftCells * AsciiSettings.CellWidth / AsciiScene.PixelsPerUnit, 0f, 0f);
            _scene.Render();

            var field = _filter.TrampleTexture!;
            var pixels = _scene.Read(field);
            var centre = pixels[(field.height / 2) * field.width + field.width / 2];
            var left = pixels[(field.height / 2) * field.width + (field.width / 2 - shiftCells)];
            Assert.Less(centre.r, 40, "the trail must not stay glued to screen centre after the camera moves");
            Assert.Greater(left.r, 180, "the crushed grass should scroll with the world");
        }
    }
}
