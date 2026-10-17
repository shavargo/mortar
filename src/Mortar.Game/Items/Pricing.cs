using Mortar.Game.Content;

namespace Mortar.Game.Items;

internal static class Pricing
{
    public static int BuyPrice(Item item, GameRules rules)
    {
        var price = item.Definition.Value;
        return item.Quality == ItemQuality.Magic ? price * rules.VendorMagicBuyMult : price;
    }

    public static int SellPrice(Item item, GameRules rules) =>
        BuyPrice(item, rules) * rules.VendorSellPercent / 100;
}
