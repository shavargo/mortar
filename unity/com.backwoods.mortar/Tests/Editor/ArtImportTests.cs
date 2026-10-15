using Mortar.Unity.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace Mortar.Unity.Tests
{
    public sealed class ArtImportTests
    {
        [TestCase("Assets/Art/tiles/cathedral/floor-0.png", "tiles/cathedral/floor-0")]
        [TestCase("Assets/Art/actors/warrior.png", "actors/warrior")]
        [TestCase("Assets\\Art\\icons\\gold.PNG", "icons/gold")]
        public void KeysAreArtRelativePathsWithoutExtension(string path, string expected)
        {
            Assert.AreEqual(expected, SpriteCatalogBuilder.KeyFor(path, SpriteCatalogBuilder.ArtRoot));
        }

        [TestCase("Assets/Other/actors/warrior.png")]
        [TestCase("Assets/Art/SpriteCatalog.asset")]
        [TestCase("Assets/Artwork/actors/warrior.png")]
        public void PathsOutsideTheArtFolderHaveNoKey(string path)
        {
            Assert.IsNull(SpriteCatalogBuilder.KeyFor(path, SpriteCatalogBuilder.ArtRoot));
        }

        [Test]
        public void FlatTilesPivotOnTheirCenter()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.5f), ArtImport.PivotFor("Assets/Art/tiles/town/grass-0.png", 64, 32));
        }

        [Test]
        public void RaisedTilesPivotOnTheCenterOfTheirBase()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.25f), ArtImport.PivotFor("Assets/Art/tiles/town/wall-0.png", 64, 64));
        }

        [Test]
        public void ActorsPivotAtTheirFeet()
        {
            Assert.AreEqual(new Vector2(0.5f, 4f / 96f), ArtImport.PivotFor("Assets/Art/actors/warrior.png", 64, 96));
        }

        [Test]
        public void IconsPivotOnTheirCenter()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.5f), ArtImport.PivotFor("Assets/Art/icons/gold.png", 64, 64));
        }

        [Test]
        public void ActorPngsWithAJsonSidecarAreSheets()
        {
            Assert.IsTrue(ArtImport.IsSheet("Assets/Art/actors/warrior/idle.png", path => path == "Assets/Art/actors/warrior/idle.json"));
        }

        [Test]
        public void PngsWithoutASidecarAreNotSheets()
        {
            Assert.IsFalse(ArtImport.IsSheet("Assets/Art/actors/warrior.png", path => false));
        }

        [TestCase("Assets/Art/icons/gold.png")]
        [TestCase("Assets/Other/actors/warrior/idle.png")]
        public void OnlyActorArtCanBeASheet(string path)
        {
            Assert.IsFalse(ArtImport.IsSheet(path, candidate => true));
        }
    }
}
