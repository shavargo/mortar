using System;
using System.Collections.Generic;
using Mortar.Game;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mortar.Unity.Tests
{
    public sealed class ActorClipTests
    {
        private readonly List<Object> _created = new List<Object>();
        private ActorClip _clip = null!;

        [SetUp]
        public void SetUp()
        {
            var sheet = MakeSheet(3 * TestClipInfo.FrameWidth, 8 * TestClipInfo.FrameHeight);
            _clip = new ActorClip("actors/warrior/attack", sheet, TestClipInfo.Make(3, 8, hitFrame: 1));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }
            _created.Clear();
        }

        [Test]
        public void SouthIsTheTopRow()
        {
            Assert.AreEqual(new Rect(0, 42, 4, 6), Frame(_clip, Direction.South, 0).rect);
        }

        [Test]
        public void SouthEastIsTheBottomRow()
        {
            Assert.AreEqual(new Rect(8, 0, 4, 6), Frame(_clip, Direction.SouthEast, 2).rect);
        }

        [Test]
        public void PivotIsMeasuredFromTheBottomLeftOfTheFrame()
        {
            var sprite = Frame(_clip, Direction.West, 1);

            Assert.AreEqual(new Vector2(2, 1), sprite.pivot);
            Assert.AreEqual(ActorClip.PixelsPerUnit, sprite.pixelsPerUnit);
        }

        [Test]
        public void FramesAreCreatedOnce()
        {
            Assert.AreSame(Frame(_clip, Direction.North, 1), Frame(_clip, Direction.North, 1));
        }

        [Test]
        public void IndexIsClampedToTheClip()
        {
            Assert.AreEqual(8f, Frame(_clip, Direction.South, 9).rect.x);
            Assert.AreEqual(0f, Frame(_clip, Direction.South, -1).rect.x);
        }

        [Test]
        public void SingleRowClipsUseTheirRowForEveryDirection()
        {
            var sheet = MakeSheet(2 * TestClipInfo.FrameWidth, TestClipInfo.FrameHeight);
            var clip = new ActorClip("actors/smith/idle", sheet, TestClipInfo.Make(2, 1, loop: true));

            Assert.AreEqual(new Rect(4, 0, 4, 6), Frame(clip, Direction.North, 1).rect);
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
        [TestCase("{\"frames\":2,\"directions\":8,\"fps\":10,\"frameWidth\":0,\"frameHeight\":6}")]
        [TestCase("{\"frames\":2,\"directions\":8,\"fps\":10,\"hitFrame\":2,\"frameWidth\":4,\"frameHeight\":6}")]
        [TestCase("{\"frames\":2,\"directions\":8,\"fps\":10,\"hitFrame\":-1,\"frameWidth\":4,\"frameHeight\":6}")]
        [TestCase("{\"frames\":2,\"directions\":8,\"fps\":10,\"frameWidth\":4,\"frameHeight\":6,\"pivotX\":5}")]
        [TestCase("{\"frames\":2,\"directions\":8,\"fps\":10,\"frameWidth\":4,\"frameHeight\":6,\"pivotY\":-1}")]
        public void BadMetadataIsRejected(string json)
        {
            Assert.Throws<FormatException>(() => ClipInfo.FromJson(json));
        }

        [Test]
        public void SheetsMatchingTheMetadataPass()
        {
            Assert.DoesNotThrow(() => TestClipInfo.Make(3, 8).CheckSheet(12, 48, "Assets/Art/actors/warrior/attack.png"));
        }

        [TestCase(12, 42)]
        [TestCase(8, 48)]
        public void SheetsOfTheWrongSizeAreRejected(int width, int height)
        {
            var error = Assert.Throws<FormatException>(
                () => TestClipInfo.Make(3, 8).CheckSheet(width, height, "Assets/Art/actors/warrior/attack.png"));

            StringAssert.Contains("Assets/Art/actors/warrior/attack.png", error!.Message);
        }

        private Texture2D MakeSheet(int width, int height)
        {
            var sheet = new Texture2D(width, height);
            _created.Add(sheet);
            return sheet;
        }

        private Sprite Frame(ActorClip clip, Direction direction, int index)
        {
            var sprite = clip.Frame(direction, index);
            if (!_created.Contains(sprite))
            {
                _created.Add(sprite);
            }
            return sprite;
        }
    }
}
