namespace Mortar.Unity
{
    public readonly struct ClipFrame
    {
        public ClipFrame(string name, int index)
        {
            Name = name;
            Index = index;
        }

        public string Name { get; }

        public int Index { get; }
    }
}
