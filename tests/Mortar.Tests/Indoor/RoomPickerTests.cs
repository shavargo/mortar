using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Tests.Support;

namespace Mortar.Tests.Indoor;

public class RoomPickerTests
{
    private static Room Hall(string id, int weight) =>
        TestContent.CompileRoom(id, new[] { "filler" }, TestContent.Box(9, 9, new[] { 4 }), weight);

    [Fact]
    public void Pick_AllWeightsOne_MatchesUniformPick()
    {
        var rooms = new[] { Hall("a", 1), Hall("b", 1), Hall("c", 1) };
        var picker = new Pcg32(7, 7);
        var uniform = new Pcg32(7, 7);
        for (var i = 0; i < 50; i++)
        {
            Assert.Same(uniform.Pick(rooms), RoomPicker.Pick(rooms, picker));
        }
    }

    [Fact]
    public void Pick_FollowsWeights()
    {
        var rooms = new[] { Hall("light", 1), Hall("heavy", 99) };
        var rng = new Pcg32(3, 3);
        var heavy = Enumerable.Range(0, 1000).Count(_ => RoomPicker.Pick(rooms, rng).Id == "heavy");
        Assert.InRange(heavy, 950, 1000);
    }

    [Fact]
    public void Pick_IsDeterministic()
    {
        var rooms = new[] { Hall("a", 2), Hall("b", 5), Hall("c", 1) };
        var first = new Pcg32(11, 4);
        var second = new Pcg32(11, 4);
        for (var i = 0; i < 50; i++)
        {
            Assert.Same(RoomPicker.Pick(rooms, first), RoomPicker.Pick(rooms, second));
        }
    }
}
