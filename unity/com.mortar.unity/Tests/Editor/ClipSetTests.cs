using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class ClipSetTests
    {
        private SpriteCatalog _catalog = null!;

        [SetUp]
        public void SetUp() => _catalog = ScriptableObject.CreateInstance<SpriteCatalog>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_catalog);

        private static ActorClip Clip(string set, string name, int frames = 2) =>
            new ActorClip(SpriteKeys.Clip(set, name), null, TestClipInfo.Make(frames, 8));

        [Test]
        public void NoIdleClipMeansNoSet()
        {
            _catalog.SetClips(new[] { Clip("zombie", ClipNames.Walk) });

            Assert.IsNull(ClipSet.For(_catalog, "zombie"));
        }

        [Test]
        public void TheSetHoldsTheClipsFoundAndFallsBackToIdle()
        {
            var idle = Clip("zombie", ClipNames.Idle);
            var walk = Clip("zombie", ClipNames.Walk);
            _catalog.SetClips(new[] { idle, walk, Clip("skeleton", ClipNames.Attack) });

            var set = ClipSet.For(_catalog, "zombie")!;

            Assert.AreSame(walk, set.Get(ClipNames.Walk));
            Assert.IsTrue(set.Has(ClipNames.Walk));
            Assert.IsFalse(set.Has(ClipNames.Attack));
            Assert.AreSame(idle, set.Get(ClipNames.Attack));
        }

        [Test]
        public void ASetNeedsAnIdleClip()
        {
            var clips = new Dictionary<string, ActorClip> { { ClipNames.Walk, Clip("zombie", ClipNames.Walk) } };

            Assert.Throws<ArgumentException>(() => new ClipSet(clips));
        }
    }
}
