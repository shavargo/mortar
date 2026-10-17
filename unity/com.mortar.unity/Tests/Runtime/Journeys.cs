using System;
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Commands;
using Mortar.Game.Simulation;
using NUnit.Framework;

namespace Mortar.Unity.Tests
{
    public static class Journeys
    {
        public const int MaxTicks = 4000;

        public static Cell MarkerCell(World world, string kind) =>
            world.Level.FindMarker(kind) ?? throw new InvalidOperationException($"depth {world.Depth} has no '{kind}' marker");

        public static void WalkNextTo(MortarGame game, string markerKind)
        {
            var world = game.Session!.World;
            var target = MarkerCell(world, markerKind);
            game.Enqueue(new MoveCommand(target));
            for (var i = 0; i < MaxTicks; i++)
            {
                game.RunTick();
                var player = world.Player;
                if (player.Cell.DistanceTo(target) <= 1 && player.Action == ActionKind.Idle && player.Intent == IntentKind.None)
                {
                    return;
                }
            }
            Assert.Fail($"player did not reach the '{markerKind}' marker within {MaxTicks} ticks");
        }

        public static void TakeStairsDown(MortarGame game)
        {
            var world = game.Session!.World;
            var depth = world.Depth;
            WalkNextTo(game, MarkerKinds.Exit);
            game.Enqueue(new UseStairsCommand());
            game.RunTick();
            Assert.AreEqual(depth + 1, world.Depth, "the stairs did not lead down");
        }
    }
}
