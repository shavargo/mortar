using System.Linq;
using Mortar.Game.Simulation;
using Mortar.Json.Content;
using Mortar.Json.Saves;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class ContentSpikeTests
    {
        [Test]
        public void ShippedContentLoadsAndCreatesAWorldInTown()
        {
            var loaded = GameContentLoader.Load(ContentPaths.Manifest);
            Assert.IsTrue(loaded.Succeeded, string.Join("\n", loaded.Errors.Select(error => error.ToString())));

            var result = World.Create(loaded.Value!.Content, loaded.Value.CreateLevelGenerator(), "warrior", 7);

            Assert.IsTrue(result.Succeeded, result.Error);
            Assert.AreEqual(0, result.World!.Depth);
        }

        [Test]
        public void SaveJsonRoundTripsToTheSameWorldHash()
        {
            var content = GameContentLoader.Load(ContentPaths.Manifest).Value!;
            var world = World.Create(content.Content, content.CreateLevelGenerator(), "warrior", 7).World!;

            var restored = SaveFile.FromJson(SaveFile.ToJson(world), content.Content, content.CreateLevelGenerator());

            Assert.IsTrue(restored.Succeeded, restored.Error);
            Assert.AreEqual(WorldHash.Compute(world), WorldHash.Compute(restored.World!));
        }
    }
}
