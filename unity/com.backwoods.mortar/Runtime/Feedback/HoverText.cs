using Mortar.Game;
using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public static class HoverText
    {
        public static string? Describe(World world, Cell cell, int pickedMonsterId = 0)
        {
            var query = new WorldCellQuery(world);
            var monster = world.Level.Find(pickedMonsterId != 0 ? pickedMonsterId : query.LivingMonsterAt(cell));
            if (monster != null)
            {
                var name = world.Content.TryGetMonster(monster.DefinitionId, out var definition) ? definition.Name : monster.DefinitionId;
                return $"{name} ({monster.Life}/{monster.Profile.MaxLife})";
            }
            var groundId = query.GroundItemAt(cell);
            return groundId == 0 ? null : world.Level.FindGroundItem(groundId)?.Describe();
        }
    }
}
