using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class GlyphLookupsTests
    {
        [Test]
        public void TheRampLookupIsBlankInTheDarkAndDensestAtFullBrightness()
        {
            var ramp = GlyphLookups.Ramp(0.06f);
            var values = ramp.GetRawTextureData();

            Assert.AreEqual(GlyphLookups.RampWidth, ramp.width);
            Assert.AreEqual(FilterMode.Point, ramp.filterMode);
            Assert.AreEqual(GlyphRules.Blank, values[0] / GlyphLookups.SlotStep);
            Assert.AreEqual(AsciiSettings.RampSize - 1, values[GlyphLookups.RampWidth - 1] / GlyphLookups.SlotStep);
            Object.DestroyImmediate(ramp);
        }

        [Test]
        public void TheEdgeLookupMatchesTheRuleAtEachTexel()
        {
            var edges = GlyphLookups.Edges(0.25f);
            var values = edges.GetRawTextureData();

            for (var y = 0; y < GlyphLookups.EdgeSize; y += 7)
            {
                for (var x = 0; x < GlyphLookups.EdgeSize; x += 7)
                {
                    var expected = GlyphRules.EdgeSlot(GlyphLookups.Gradient(x), GlyphLookups.Gradient(y), 0.25f);
                    Assert.AreEqual(expected, values[y * GlyphLookups.EdgeSize + x] / GlyphLookups.SlotStep, $"texel {x},{y}");
                }
            }
            Object.DestroyImmediate(edges);
        }

        [Test]
        public void AVerticalBoundaryLooksUpTheBarGlyph()
        {
            var edges = GlyphLookups.Edges(0.25f);

            Assert.AreEqual(AsciiSettings.FirstEdgeSlot + 2, edges.GetRawTextureData()[31 * GlyphLookups.EdgeSize + 16] / GlyphLookups.SlotStep);
            Object.DestroyImmediate(edges);
        }

        [TestCase(0f, 1f)]
        [TestCase(0.55f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(2f, 0f)]
        public void LightFallsOffFromTheInnerRadiusToTheEdge(float distance, float expected)
        {
            Assert.AreEqual(expected, LightFalloff.At(distance), 1e-5f);
        }

        [Test]
        public void LightFallsOffSmoothly()
        {
            var previous = 1f;
            for (var d = 0f; d <= 1f; d += 0.05f)
            {
                var value = LightFalloff.At(d);
                Assert.LessOrEqual(value, previous + 1e-6f);
                previous = value;
            }
            Assert.That(LightFalloff.At(0.8f), Is.InRange(0.1f, 0.9f));
        }

        [Test]
        public void TheFalloffLookupStartsBrightAndEndsDark()
        {
            var falloff = GlyphLookups.Falloff();
            var values = falloff.GetRawTextureData();

            Assert.AreEqual(GlyphLookups.FalloffWidth, falloff.width);
            Assert.AreEqual(FilterMode.Bilinear, falloff.filterMode);
            Assert.AreEqual(255, values[0]);
            Assert.LessOrEqual(values[GlyphLookups.FalloffWidth - 1], 1);
            Object.DestroyImmediate(falloff);
        }
    }
}
