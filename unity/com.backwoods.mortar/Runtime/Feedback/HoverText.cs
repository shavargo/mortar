using Mortar.Game;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public static class HoverText
    {
        public static string? Describe(World world, Cell cell)
        {
            var query = new WorldCellQuery(world);
            var monsterId = query.LivingMonsterAt(cell);
            if (monsterId != 0)
            {
                var monster = world.Level.Find(monsterId)!;
                var name = world.Content.TryGetMonster(monster.DefinitionId, out var definition) ? definition.Name : monster.DefinitionId;
                return $"{name} ({monster.Life}/{monster.Profile.MaxLife})";
            }
            var groundId = query.GroundItemAt(cell);
            return groundId == 0 ? null : world.Level.FindGroundItem(groundId)?.Describe();
        }
    }
}
