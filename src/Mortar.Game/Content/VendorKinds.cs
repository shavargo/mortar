using Mortar.Core.Model;

namespace Mortar.Game.Content;

public static class VendorKinds
{
    public const string Smith = MarkerKinds.Smith;
    public const string Healer = MarkerKinds.Healer;
    public const string Sage = MarkerKinds.Sage;

    public static readonly IReadOnlyList<string> All = MarkerKinds.TownNpcs;

    public static bool IsKnown(string kind) => MarkerKinds.IsTownNpc(kind);

    public static bool Accepts(string vendorKind, string slot) =>
        vendorKind switch
        {
            Smith => IsSmithSlot(slot),
            Healer => slot == ItemSlots.Potion,
            Sage => slot == ItemSlots.Scroll || slot == ItemSlots.Book,
            _ => false,
        };

    private static bool IsSmithSlot(string slot) =>
        slot == ItemSlots.Weapon
        || slot == ItemSlots.Body
        || slot == ItemSlots.Head
        || slot == ItemSlots.Shield
        || slot == ItemSlots.Ring
        || slot == ItemSlots.Amulet;
}
