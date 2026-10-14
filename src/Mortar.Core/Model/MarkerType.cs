namespace Mortar.Core.Model;

public sealed class MarkerType
{
    public MarkerType(char symbol, string kind)
    {
        Symbol = symbol;
        Kind = kind;
    }

    public char Symbol { get; }

    public string Kind { get; }
}
