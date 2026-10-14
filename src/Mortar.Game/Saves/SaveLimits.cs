namespace Mortar.Game.Saves;

/// <summary>Upper bounds on saved counters and totals. Play cannot reach them, and they leave room so later arithmetic cannot wrap.</summary>
internal static class SaveLimits
{
    public const int MaxId = int.MaxValue / 2;

    public const long MaxTotal = long.MaxValue / 2;
}
