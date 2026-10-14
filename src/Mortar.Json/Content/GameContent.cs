using Mortar.Core.Model;
using Mortar.Game.Content;
using Mortar.Game.Simulation;

namespace Mortar.Json.Content;

public sealed class GameContent
{
    public GameContent(ContentSet content, IReadOnlyDictionary<string, CompiledArea> areas)
    {
        Content = content;
        Areas = areas;
    }

    public ContentSet Content { get; }

    public IReadOnlyDictionary<string, CompiledArea> Areas { get; }

    public ILevelGenerator CreateLevelGenerator() => new AreaLevelGenerator(Areas);
}
