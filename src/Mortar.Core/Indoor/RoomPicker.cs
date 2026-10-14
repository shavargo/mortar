using Mortar.Core.Model;
using Mortar.Core.Rng;

namespace Mortar.Core.Indoor;

internal static class RoomPicker
{
    /// <summary>Uses the plain uniform pick when every weight is 1, so layouts from unweighted rooms never change.</summary>
    public static Room Pick(IReadOnlyList<Room> rooms, Pcg32 rng) =>
        rooms.All(room => room.Weight == 1)
            ? rng.Pick(rooms)
            : rooms[rng.PickWeighted(rooms.Select(room => room.Weight).ToList())];
}
