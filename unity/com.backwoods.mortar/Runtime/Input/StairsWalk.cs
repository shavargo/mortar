using Mortar.Game;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public static class StairsWalk
    {
        public static bool IsNextToStairs(World world)
        {
            var cell = world.Player.Cell;
            foreach (var marker in world.Level.Layout.Markers)
            {
                if (WorldCellQuery.IsStairsKind(marker.Kind) && cell.DistanceTo(new Cell(marker.X, marker.Y)) <= 1)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsSettled(Actor player) => player.Action == ActionKind.Idle && player.Intent == IntentKind.None;
    }
}
