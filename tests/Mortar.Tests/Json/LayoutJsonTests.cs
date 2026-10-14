using System.Text.Json.Nodes;
using Mortar.Core.Generation;
using Mortar.Core.Layouts;
using Mortar.Json;
using Mortar.Tests.Support;

namespace Mortar.Tests.Json;

public class LayoutJsonTests
{
    private static readonly Layout Sample = GenerateSample();

    private static Layout GenerateSample()
    {
        var result = Generator.Generate(SampleArea.LoadCrypt(), 1);
        Assert.True(result.Succeeded, result.FailureSummary);
        return result.Layout!;
    }

    private static JsonNode SampleNode() => JsonNode.Parse(LayoutWriter.ToJson(Sample))!;

    [Fact]
    public void RoundTrip_PreservesLayout()
    {
        var read = LayoutReader.FromJson(LayoutWriter.ToJson(Sample));
        Assert.True(read.Succeeded, string.Join("; ", read.Errors));
        var layout = read.Value!;
        Assert.Equal(Sample.Fingerprint, layout.Fingerprint);
        Assert.Equal(Sample.Cells.Ground, layout.Cells.Ground);
        Assert.Equal(Sample.Cells.Walkable, layout.Cells.Walkable);
        Assert.Equal(
            Sample.Markers.Select(m => (m.Kind, m.NodeId, m.X, m.Y)),
            layout.Markers.Select(m => (m.Kind, m.NodeId, m.X, m.Y)));
    }

    [Fact]
    public void ToJson_IsDeterministic()
    {
        var json = LayoutWriter.ToJson(Sample);
        Assert.Equal(json, LayoutWriter.ToJson(Sample));
        Assert.StartsWith("{\"formatVersion\":1,\"mortarVersion\":\"0.1.0\",\"areaId\":\"crypt\"", json);
    }

    [Fact]
    public void FromJson_TamperedCell_ReportsFingerprintMismatch()
    {
        var node = SampleNode();
        node["cells"]!["variant"]![0] = 7;
        var error = Assert.Single(LayoutReader.FromJson(node.ToJsonString()).Errors);
        Assert.StartsWith("fingerprint mismatch", error.Message);
    }

    [Fact]
    public void FromJson_UnsupportedFormatVersion_IsAnError()
    {
        var node = SampleNode();
        node["formatVersion"] = 99;
        var error = Assert.Single(LayoutReader.FromJson(node.ToJsonString()).Errors);
        Assert.StartsWith("unsupported formatVersion 99", error.Message);
    }

    [Fact]
    public void FromJson_Truncated_IsInvalidJson() =>
        Assert.StartsWith("invalid JSON", Assert.Single(LayoutReader.FromJson("{\"formatVersion\":").Errors).Message);

    [Fact]
    public void FromJson_MissingProperties_IsMalformed() =>
        Assert.StartsWith("malformed layout", Assert.Single(LayoutReader.FromJson("{\"formatVersion\":1}").Errors).Message);
}
