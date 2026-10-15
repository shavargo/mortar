using System;
using Mortar.Game;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class ActorClipTests
    {
        private Texture2D _sheet = null!;
        private ActorClip _clip = null!;

        [SetUp]
        public void SetUp()
        {
            _sheet = new Texture2D(3 * TestClipInfo.FrameWidth, 8 * TestClipInfo.FrameHeight);
            _clip = new ActorClip("actors/warrior/attack", _sheet, TestClipInfo.Make(3, 8, hitFrame: 1));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_sheet);

        [Test]
        public void SouthIsTheTopRow()
        {
            Assert.AreEqual(new Rect(0, 42, 4, 6), _clip.Frame(Direction.South, 0).rect);
        }

        [Test]
        public void SouthEastIsTheBottomRow()
        {
            Assert.AreEqual(new Rect(8, 0, 4, 6), _clip.Frame(Direction.SouthEast, 2).rect);
        }

        [Test]
        public void PivotIsMeasuredFromTheBottomLeftOfTheFrame()
        {
            var sprite = _clip.Frame(Direction.West, 1);

            Assert.AreEqual(new Vector2(2, 1), sprite.pivot);
            Assert.AreEqual(ActorClip.PixelsPerUnit, sprite.pixelsPerUnit);
        }

        [Test]
        public void FramesAreCreatedOnce()
        {
            Assert.AreSame(_clip.Frame(Direction.North, 1), _clip.Frame(Direction.North, 1));
        }

        [Test]
        public void IndexIsClampedToTheClip()
        {
            Assert.AreEqual(8f, _clip.Frame(Direction.South, 9).rect.x);
            Assert.AreEqual(0f, _clip.Frame(Direction.South, -1).rect.x);
        }

        [Test]
        public void SingleRowClipsUseTheirRowForEveryDirection()
        {
            var sheet = new Texture2D(2 * TestClipInfo.FrameWidth, TestClipInfo.FrameHeight);
            var clip = new ActorClip("actors/smith/idle", sheet, TestClipInfo.Make(2, 1, loop: true));

            Assert.AreEqual(new Rect(4, 0, 4, 6), clip.Frame(Direction.North, 1).rect);
            Object.DestroyImmediate(sheet);
        }

        [Test]
        public void MissingSheetFallsBackToTheActorPlaceholder()
        {
            var clip = new ActorClip("actors/warrior/idle", null, TestClipInfo.Make(2, 8));

            Assert.AreSame(GeneratedSprites.Actor, clip.Frame(Direction.South, 0));
        }

        [Test]
        public void MetadataComesFromTheSidecar()
        {
            Assert.AreEqual(3, _clip.Frames);
            Assert.AreEqual(1, _clip.HitFrame);
            Assert.AreEqual(0.3f, _clip.Seconds, 1e-5f);
            Assert.IsFalse(_clip.Loop);
        }

        [TestCase("{\"frames\":2,\"directions\":3,\"fps\":10,\"frameWidth\":4,\"frameHeight\":6}")]
        [TestCase("{\"frames\":0,\"directions\":8,\"fps\":10,\"frameWidth\":4,\"frameHeight\":6}")]
        [TestCase("{\"frames\":2,\"directions\":8,\"fps\":0,\"frameWidth\":4,\"frameHeight\":6}")]
        public void BadMetadataIsRejected(string json)
        {
            Assert.Throws<FormatException>(() => ClipInfo.FromJson(json));
        }
    }
}
