using System.Diagnostics.CodeAnalysis;

namespace Mortar.Core.Model;

public static class MarkerChars
{
    public const char Door = 'D';

    public static readonly IReadOnlyList<MarkerType> BuiltIn = new[]
    {
        new MarkerType('E', MarkerKinds.Entrance),
        new MarkerType('X', MarkerKinds.Exit),
        new MarkerType('B', MarkerKinds.Boss),
        new MarkerType('W', MarkerKinds.Waypoint),
        new MarkerType('S', MarkerKinds.Spawn),
        new MarkerType('C', MarkerKinds.Chest),
    };

    public static bool TryGetKind(char symbol, [NotNullWhen(true)] out string? kind)
    {
        kind = BuiltIn.FirstOrDefault(marker => marker.Symbol == symbol)?.Kind;
        return kind is not null;
    }

    public static bool IsReserved(char symbol) => symbol == Door || BuiltIn.Any(marker => marker.Symbol == symbol);

    public static bool IsBuiltInKind(string kind) => BuiltIn.Any(marker => marker.Kind == kind);
}
