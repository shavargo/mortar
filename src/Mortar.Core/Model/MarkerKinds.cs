namespace Mortar.Core.Model;

public static class MarkerKinds
{
    public const string Entrance = "entrance";
    public const string Exit = "exit";
    public const string Boss = "boss";
    public const string Waypoint = "waypoint";
    public const string Spawn = "spawn";
    public const string Chest = "chest";

    public static readonly IReadOnlyList<string> Landmarks = new[] { Entrance, Exit, Boss, Waypoint };
}
