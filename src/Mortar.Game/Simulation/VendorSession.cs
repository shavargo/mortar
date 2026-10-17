using Mortar.Game.Content;
using Mortar.Game.Events;

namespace Mortar.Game.Simulation;

internal static class VendorSession
{
    public static string? Talk(World world, string vendorKind)
    {
        if (!VendorKinds.IsKnown(vendorKind)) return "unknown vendor";
        if (world.Depth != 0) return "not in town";
        if (!IsNear(world, vendorKind)) return "no vendor within reach";
        world.ActiveVendor = vendorKind;
        return null;
    }

    public static string? Leave(World world)
    {
        if (world.ActiveVendor is null) return "no active vendor";
        world.ActiveVendor = null;
        return null;
    }

    public static string? RequireActive(World world)
    {
        if (world.ActiveVendor is null) return "no active vendor";
        if (world.Depth != 0) return "not in town";
        if (!IsNear(world, world.ActiveVendor)) return "no vendor within reach";
        return null;
    }

    public static void Update(World world, EventSink events)
    {
        var kind = world.ActiveVendor;
        if (kind is null) return;
        if (world.Depth == 0 && IsNear(world, kind)) return;
        world.ActiveVendor = null;
        events.Add(new VendorSessionEnded(events.Tick, kind));
    }

    private static bool IsNear(World world, string vendorKind)
    {
        var marker = world.Level.FindMarker(vendorKind);
        return marker is not null && world.Player.Cell.DistanceTo(marker.Value) <= 1;
    }
}
