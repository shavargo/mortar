namespace Mortar.Game.Content;

public sealed class LootTableDefinition
{
    public LootTableDefinition(string id, int rolls, int noneWeight, int goldWeight, int itemWeight, IntRange gold, int magicPercent)
    {
        Id = id;
        Rolls = rolls;
        NoneWeight = noneWeight;
        GoldWeight = goldWeight;
        ItemWeight = itemWeight;
        Gold = gold;
        MagicPercent = magicPercent;
    }

    public string Id { get; }

    public int Rolls { get; }

    public int NoneWeight { get; }

    public int GoldWeight { get; }

    public int ItemWeight { get; }

    public IntRange Gold { get; }

    public int MagicPercent { get; }
}
