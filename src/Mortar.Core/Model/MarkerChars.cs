using System.Diagnostics.CodeAnalysis;

namespace Mortar.Core.Model;

public static class MarkerChars
{
    public const char Door = 'D';

    private static readonly Dictionary<char, string> KindsByChar = new Dictionary<char, string>
    {
        ['E'] = MarkerKinds.Entrance,
        ['X'] = MarkerKinds.Exit,
        ['B'] = MarkerKinds.Boss,
        ['W'] = MarkerKinds.Waypoint,
        ['S'] = MarkerKinds.Spawn,
        ['C'] = MarkerKinds.Chest,
    };

    public static bool TryGetKind(char symbol, [NotNullWhen(true)] out string? kind) =>
        KindsByChar.TryGetValue(symbol, out kind);

    public static bool IsReserved(char symbol) => symbol == Door || KindsByChar.ContainsKey(symbol);
}
