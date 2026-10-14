using Mortar.Core;
using Mortar.Core.Layouts;

namespace Mortar.Tests.Layouts;

public class FingerprintTests
{
    private static LayoutCells Cells(int variant) => new LayoutCells(
        2,
        1,
        new[] { "void", "floor" },
        new[] { 1, 1 },
        new[] { "floor" },
        new[] { 0, 0 },
        new[] { 0, variant },
        new[] { true, true });

    [Fact]
    public void Compute_IsStableAndHex()
    {
        var first = LayoutFingerprint.Compute("crypt", 1, 0, Cells(0));
        Assert.Equal(first, LayoutFingerprint.Compute("crypt", 1, 0, Cells(0)));
        Assert.Matches("^[0-9a-f]{16}$", first);
    }

    [Fact]
    public void Compute_ChangesWithEveryInput()
    {
        var baseline = LayoutFingerprint.Compute("crypt", 1, 0, Cells(0));
        Assert.NotEqual(baseline, LayoutFingerprint.Compute("crypt", 1, 0, Cells(1)));
        Assert.NotEqual(baseline, LayoutFingerprint.Compute("crypt", 2, 0, Cells(0)));
        Assert.NotEqual(baseline, LayoutFingerprint.Compute("crypt", 1, 1, Cells(0)));
        Assert.NotEqual(baseline, LayoutFingerprint.Compute("forest", 1, 0, Cells(0)));
    }

    [Fact]
    public void Layout_ComputesItsOwnFingerprint()
    {
        var layout = new Layout("crypt", 1, 0, Cells(0), Array.Empty<Marker>(), MortarInfo.Version, Layout.CurrentFormatVersion);
        Assert.Equal(LayoutFingerprint.Compute("crypt", 1, 0, Cells(0)), layout.Fingerprint);
        Assert.Equal(2, layout.Width);
    }
}
