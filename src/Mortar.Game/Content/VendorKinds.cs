namespace Mortar.Game.Content;

public static class VendorKinds
{
    public const string Smith = "smith";
    public const string Healer = "healer";
    public const string Sage = "sage";

    public static readonly IReadOnlyList<string> All = new[] { Smith, Healer, Sage };

    public static bool IsKnown(string kind) =>
        kind == Smith || kind == Healer || kind == Sage;
}
