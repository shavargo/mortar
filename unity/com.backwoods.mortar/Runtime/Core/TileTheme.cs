namespace Mortar.Unity
{
    public static class TileTheme
    {
        public static string For(string areaId)
        {
            var dash = areaId.LastIndexOf('-');
            if (dash <= 0 || dash == areaId.Length - 1)
            {
                return areaId;
            }
            for (var i = dash + 1; i < areaId.Length; i++)
            {
                if (areaId[i] < '0' || areaId[i] > '9')
                {
                    return areaId;
                }
            }
            return areaId.Substring(0, dash);
        }
    }
}
