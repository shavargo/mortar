using System;
using System.IO;
using Mortar.Json.Content;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public static class TestContent
    {
        private static GameContent? _content;

        public static GameContent Content
        {
            get
            {
                if (_content == null)
                {
                    _content = ContentLoading.Load(ContentPaths.Manifest, out var error);
                    if (_content == null)
                    {
                        Assert.Fail(error);
                    }
                }
                return _content!;
            }
        }

        public static GameSession NewSession(uint seed = 7)
        {
            var session = GameSession.NewGame(Content, "warrior", seed, out var error);
            Assert.IsNotNull(session, error);
            return session!;
        }
    }

    public static class TestPaths
    {
        public static string FreshSaveFolder() =>
            Path.Combine(Application.temporaryCachePath, "mortar-tests", Guid.NewGuid().ToString("N"));

        public static void Delete(string folder)
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
