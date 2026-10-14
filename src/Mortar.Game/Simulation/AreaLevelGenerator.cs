using Mortar.Core.Generation;
using Mortar.Core.Model;
using Mortar.Game.Content;

namespace Mortar.Game.Simulation;

public sealed class AreaLevelGenerator : ILevelGenerator
{
    private readonly IReadOnlyDictionary<string, CompiledArea> _areas;

    public AreaLevelGenerator(IReadOnlyDictionary<string, CompiledArea> areas)
    {
        _areas = areas;
    }

    public LevelBuildResult Generate(LevelDefinition level, uint seed)
    {
        if (!_areas.TryGetValue(level.AreaId, out var area))
        {
            return LevelBuildResult.Failure($"unknown area '{level.AreaId}'");
        }
        var result = Generator.Generate(area, seed);
        return result.Succeeded
            ? LevelBuildResult.Success(result.Layout!)
            : LevelBuildResult.Failure(result.FailureSummary);
    }
}
