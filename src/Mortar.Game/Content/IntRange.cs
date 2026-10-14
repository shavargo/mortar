namespace Mortar.Game.Content;

public readonly struct IntRange
{
    public IntRange(int min, int max)
    {
        Min = min;
        Max = max;
    }

    public int Min { get; }

    public int Max { get; }

    public IntRange Shift(int amount) => new IntRange(Min + amount, Max + amount);

    public override string ToString() => $"{Min}-{Max}";
}
