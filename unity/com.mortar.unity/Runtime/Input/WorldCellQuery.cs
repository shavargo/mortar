using Mortar.Game;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public sealed class WorldCellQuery : ICellQuery
    {
        private readonly World _world;

        public WorldCellQuery(World world)
        {
            _world = world;
        }

        public int LivingMonsterAt(Cell cell)
        {
            var level = _world.Level;
            var actor = level.Find(level.OccupantAt(cell));
            return actor != null && actor.Kind == ActorKind.Monster && actor.IsAlive ? actor.Id : 0;
        }

        public int GroundItemAt(Cell cell)
        {
            foreach (var ground in _world.Level.GroundItems)
            {
                if (ground.Cell == cell)
                {
                    return ground.Id;
                }
            }
            return 0;
        }

        public bool IsStairs(Cell cell)
        {
            foreach (var marker in _world.Level.Layout.Markers)
            {
                if (marker.X == cell.X && marker.Y == cell.Y && StairsWalk.IsStairs(_world, marker.Kind))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsWalkable(Cell cell) => _world.Level.IsFree(cell);
    }
}
