using System.Collections.Generic;
using Mortar.Game.Content;
using Mortar.Game.Items;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ItemText
    {
        public static IReadOnlyList<string> Lines(Item item)
        {
            var definition = item.Definition;
            var lines = new List<string> { item.Name };
            if (definition.Damage.HasValue)
            {
                lines.Add($"Damage: {definition.Damage.Value.Min}-{definition.Damage.Value.Max}");
            }
            if (definition.Armor.HasValue)
            {
                lines.Add($"Armor: {item.ArmorValue}");
            }
            if (definition.BlockChance > 0)
            {
                lines.Add($"Block: {definition.BlockChance}%");
            }
            if (definition.Potion != null)
            {
                lines.Add($"Restores {definition.Potion.Amount} {definition.Potion.Restores}");
            }
            AddAffix(lines, item.Prefix);
            AddAffix(lines, item.Suffix);
            foreach (var kind in Stats.Kinds)
            {
                var required = definition.Requirements.Get(kind);
                if (required > 0)
                {
                    lines.Add($"Requires {kind} {required}");
                }
            }
            return lines;
        }

        public static string BonusName(string bonus)
        {
            switch (bonus)
            {
                case BonusKinds.ArmorClass:
                    return "armor class";
                case BonusKinds.ToHit:
                    return "to hit";
                default:
                    return bonus;
            }
        }

        public static Color NameColor(Item item) => item.Quality == ItemQuality.Magic ? Ui.MagicColor : Ui.TextColor;

        private static void AddAffix(List<string> lines, RolledAffix? affix)
        {
            if (affix != null)
            {
                var sign = affix.Value >= 0 ? "+" : "";
                lines.Add($"{sign}{affix.Value} {BonusName(affix.Definition.Bonus)}");
            }
        }
    }
}
