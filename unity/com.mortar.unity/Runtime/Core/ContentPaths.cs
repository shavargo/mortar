using System.IO;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ContentPaths
    {
        public static string Manifest => Path.Combine(Application.streamingAssetsPath, "content", "game.json");
    }
}
