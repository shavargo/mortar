namespace Mortar.Unity.Tests
{
    public static class TestClipInfo
    {
        public const int FrameWidth = 4;
        public const int FrameHeight = 6;

        public static ClipInfo Make(int frames, int directions, int hitFrame = 0, bool loop = false, float fps = 10f) =>
            ClipInfo.FromJson(
                $"{{\"frames\":{frames},\"directions\":{directions},\"fps\":{fps},\"loop\":{(loop ? "true" : "false")}," +
                $"\"hitFrame\":{hitFrame},\"frameWidth\":{FrameWidth},\"frameHeight\":{FrameHeight},\"pivotX\":2,\"pivotY\":1}}");
    }
}
