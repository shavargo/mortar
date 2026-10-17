using Mortar.Game.Content;

namespace Mortar.Game.Items;

internal static class Pricing
{
    public static int BuyPrice(Item item, GameRules rules)
    {
        if (item.Quality == ItemQuality.Unique)
        {
            return item.UniqueValue > 0
                ? item.UniqueValue
                : item.Definition.Value * rules.VendorMagicBuyMult;
        }
        var price = item.Definition.Value;
        return item.Quality == ItemQuality.Magic ? price * rules.VendorMagicBuyMult : price;
    }

    public static int SellPrice(Item item, GameRules rules) =>
        BuyPrice(item, rules) * rules.VendorSellPercent / 100;

    public static int RepairCost(Item item, GameRules rules)
    {
        if (item.MaxDurability <= 0 || item.Durability >= item.MaxDurability) return 0;
        var missing = item.MaxDurability - item.Durability;
        var cost = item.Definition.Value * missing * rules.VendorRepairPercent
            / (item.MaxDurability * 100);
        return cost < 1 ? 1 : cost;
    }
}
