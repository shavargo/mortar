namespace Mortar.Unity
{
    public readonly struct CellGrid
    {
        public CellGrid(int width, int height)
        {
            Columns = Count(width, AsciiSettings.CellWidth);
            Rows = Count(height, AsciiSettings.CellHeight);
        }

        public int Columns { get; }

        public int Rows { get; }

        private static int Count(int pixels, int cell) => pixels <= 0 ? 1 : (pixels + cell - 1) / cell;
    }
}
