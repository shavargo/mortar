using Mortar.Game.Commands;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public sealed class HoverTextTests
    {
        [Test]
        public void ThePlayerAndEmptyFloorHaveNoHoverText()
        {
            var world = TestContent.NewSession().World;

            Assert.IsNull(HoverText.Describe(world, world.Player.Cell));
        }

        [Test]
        public void GroundItemsShowTheirName()
        {
            var session = TestContent.NewSession();
            var potion = session.World.Belongings.Belt.Get(0)!;
            session.Enqueue(new DropItemCommand(potion.Id));
            session.Tick();
            var ground = session.World.Level.GroundItems[0];

            Assert.AreEqual(potion.Name, HoverText.Describe(session.World, ground.Cell));
        }
    }
}
