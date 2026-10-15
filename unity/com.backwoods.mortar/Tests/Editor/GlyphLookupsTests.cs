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
    }
}
