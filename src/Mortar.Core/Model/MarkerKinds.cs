namespace Mortar.Core.Model;

public static class MarkerKinds
{
    public const string Entrance = "entrance";
    public const string Exit = "exit";
    public const string Boss = "boss";
    public const string Waypoint = "waypoint";
    public const string Spawn = "spawn";
    public const string Chest = "chest";
    public const string Smith = "smith";
    public const string Healer = "healer";
    public const string Sage = "sage";

    public static readonly IReadOnlyList<string> Landmarks = new[] { Entrance, Exit, Boss, Waypoint };

    public static readonly IReadOnlyList<string> TownNpcs = new[] { Smith, Healer, Sage };

    public static bool IsTownNpc(string kind) =>
        kind == Smith || kind == Healer || kind == Sage;
}
