using Mortar.Core.Model;

namespace Mortar.Game.Content;

public static class VendorKinds
{
    public const string Smith = MarkerKinds.Smith;
    public const string Healer = MarkerKinds.Healer;
    public const string Sage = MarkerKinds.Sage;

    public static readonly IReadOnlyList<string> All = MarkerKinds.TownNpcs;

    public static bool IsKnown(string kind) => MarkerKinds.IsTownNpc(kind);
}
