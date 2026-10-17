namespace Mortar.Game.Commands;

public sealed class BuyCommand : Command
{
    public BuyCommand(int stockIndex)
    {
        StockIndex = stockIndex;
    }

    public int StockIndex { get; }

    public override string Describe() => $"buy stock {StockIndex}";
}
