using Mortar.Game;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public static class StairsWalk
    {
        public static bool IsWithinReach(Actor player, Cell stairs) => player.Cell.DistanceTo(stairs) <= 1;

        public static bool IsSettled(Actor player) => player.Action == ActionKind.Idle && player.Intent == IntentKind.None;
    }
}
