namespace Mortar.Unity
{
    public static class SpriteKeys
    {
        public static string Tile(string theme, string ground, int variant) => $"tiles/{theme}/{ground}-{variant}";

        public static string Actor(string id) => $"actors/{id}";

        public static string Icon(string id) => $"icons/{id}";

        public static string GlyphIcon(string id) => $"glyph-icons/{id}";

        public static string Clip(string id, string clip) => $"actors/{id}/{clip}";
    }
}
