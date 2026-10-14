using Mortar.Core.Authoring;

namespace Mortar.Core.Model;

public sealed class Tileset
{
    public const string FloorId = "floor";
    public const string WallId = "wall";
    public const string VoidId = "void";

    private static readonly IReadOnlyList<int> DefaultWeights = new[] { 1 };
    private static readonly string[] RequiredIds = { VoidId, FloorId, WallId };

    private readonly Dictionary<string, int> _byId = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<char, int> _bySymbol = new Dictionary<char, int>();
    private readonly IReadOnlyDictionary<string, IReadOnlyList<int>> _variantWeights;

    private Tileset(IReadOnlyList<GroundType> grounds, IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights)
    {
        Grounds = grounds;
        _variantWeights = variantWeights;
        for (var i = 0; i < grounds.Count; i++)
        {
            _byId[grounds[i].Id] = i;
            _bySymbol[grounds[i].Symbol] = i;
        }
        FloorIndex = _byId[FloorId];
        WallIndex = _byId[WallId];
        VoidIndex = _byId[VoidId];
    }

    public IReadOnlyList<GroundType> Grounds { get; }

    public int FloorIndex { get; }

    public int WallIndex { get; }

    public int VoidIndex { get; }

    public int IndexOf(string id) => _byId.TryGetValue(id, out var index) ? index : -1;

    public int IndexOfSymbol(char symbol) => _bySymbol.TryGetValue(symbol, out var index) ? index : -1;

    public bool IsWalkable(int index) => Grounds[index].Walkable;

    public IReadOnlyList<int> WeightsFor(string tileType) =>
        _variantWeights.TryGetValue(tileType, out var weights) ? weights : DefaultWeights;

    public static AuthoringResult<Tileset> Create(
        string source,
        IReadOnlyList<GroundType> grounds,
        IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights)
    {
        var errors = new List<AuthoringError>();
        CheckGrounds(source, grounds, errors);
        CheckWeights(source, variantWeights, errors);
        return errors.Count > 0
            ? AuthoringResult<Tileset>.Failure(errors)
            : AuthoringResult<Tileset>.Success(new Tileset(grounds, variantWeights));
    }

    private static void CheckGrounds(string source, IReadOnlyList<GroundType> grounds, List<AuthoringError> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var symbols = new HashSet<char>();
        foreach (var ground in grounds)
        {
            if (string.IsNullOrWhiteSpace(ground.Id))
                errors.Add(new AuthoringError(source, "a ground type has no id"));
            else if (!ids.Add(ground.Id))
                errors.Add(new AuthoringError(source, $"duplicate ground id '{ground.Id}'"));
            if (!symbols.Add(ground.Symbol))
                errors.Add(new AuthoringError(source, $"duplicate ground char '{ground.Symbol}'"));
            if (MarkerChars.IsReserved(ground.Symbol))
                errors.Add(new AuthoringError(source, $"ground '{ground.Id}' uses reserved char '{ground.Symbol}'"));
        }
        foreach (var required in RequiredIds)
        {
            if (!ids.Contains(required))
                errors.Add(new AuthoringError(source, $"missing required ground type '{required}'"));
        }
        CheckWalkability(source, grounds, errors);
    }

    private static void CheckWalkability(string source, IReadOnlyList<GroundType> grounds, List<AuthoringError> errors)
    {
        foreach (var ground in grounds)
        {
            if (ground.Id == FloorId && !ground.Walkable)
                errors.Add(new AuthoringError(source, "ground type 'floor' must be walkable"));
            if ((ground.Id == WallId || ground.Id == VoidId) && ground.Walkable)
                errors.Add(new AuthoringError(source, $"ground type '{ground.Id}' must not be walkable"));
        }
    }

    private static void CheckWeights(
        string source,
        IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights,
        List<AuthoringError> errors)
    {
        foreach (var tileType in variantWeights.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            var weights = variantWeights[tileType];
            if (weights.Count == 0 || weights.Any(w => w < 0) || weights.All(w => w == 0))
            {
                errors.Add(new AuthoringError(
                    source,
                    $"variant weights for '{tileType}' must be non-negative with at least one positive weight"));
            }
        }
    }
}
