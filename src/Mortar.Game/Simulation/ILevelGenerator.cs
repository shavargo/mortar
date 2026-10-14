using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public interface ILevelGenerator
{
    LevelBuildResult Generate(LevelDefinition level, uint seed);
}
