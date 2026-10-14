using Mortar.Game.Content;
using Mortar.Game.Items;

namespace Mortar.Tests.Gameplay.Support;

internal static class TestItems
{
    public static Item Create(
        ItemBaseDefinition definition, int id = 1, int armor = 0, RolledAffix? prefix = null, RolledAffix? suffix = null) =>
        new Item(id, definition, definition.ItemLevel, armor, prefix, suffix);

    public static RolledAffix Roll(AffixDefinition affix, int value) => new RolledAffix(affix, value);
}
