using System.Diagnostics.CodeAnalysis;
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
    private readonly Dictionary<char, string> _markerKinds = new Dictionary<char, string>();
    private readonly IReadOnlyDictionary<string, IReadOnlyList<int>> _variantWeights;

    private Tileset(
        IReadOnlyList<GroundType> grounds,
        IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights,
        IReadOnlyList<MarkerType> markers)
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
        Markers = MarkerChars.BuiltIn.Concat(markers).ToArray();
        foreach (var marker in Markers)
        {
            _markerKinds[marker.Symbol] = marker.Kind;
        }
    }

    public IReadOnlyList<GroundType> Grounds { get; }

    public int FloorIndex { get; }

    public int WallIndex { get; }

    public int VoidIndex { get; }

    public IReadOnlyList<MarkerType> Markers { get; }

    public bool TryGetMarkerKind(char symbol, [NotNullWhen(true)] out string? kind) =>
        _markerKinds.TryGetValue(symbol, out kind);

    public int IndexOf(string id) => _byId.TryGetValue(id, out var index) ? index : -1;

    public int IndexOfSymbol(char symbol) => _bySymbol.TryGetValue(symbol, out var index) ? index : -1;

    public bool IsWalkable(int index) => Grounds[index].Walkable;

    public IReadOnlyList<int> WeightsFor(string tileType) =>
        _variantWeights.TryGetValue(tileType, out var weights) ? weights : DefaultWeights;

    public static AuthoringResult<Tileset> Create(
        string source,
        IReadOnlyList<GroundType> grounds,
        IReadOnlyDictionary<string, IReadOnlyList<int>> variantWeights,
        IReadOnlyList<MarkerType>? markers = null)
    {
        var custom = markers ?? Array.Empty<MarkerType>();
        var errors = new List<AuthoringError>();
        CheckGrounds(source, grounds, errors);
        CheckWeights(source, variantWeights, errors);
        CheckMarkers(source, grounds, custom, errors);
        return errors.Count > 0
            ? AuthoringResult<Tileset>.Failure(errors)
            : AuthoringResult<Tileset>.Success(new Tileset(grounds, variantWeights, custom));
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
            else if (weights.Sum(w => (long)w) > int.MaxValue)
            {
                errors.Add(new AuthoringError(source, $"variant weights for '{tileType}' must add up to at most {int.MaxValue}"));
            }
        }
    }

    private static void CheckMarkers(
        string source,
        IReadOnlyList<GroundType> grounds,
        IReadOnlyList<MarkerType> markers,
        List<AuthoringError> errors)
    {
        var symbols = new HashSet<char>();
        var kinds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var marker in markers)
        {
            if (grounds.Any(ground => ground.Symbol == marker.Symbol))
                errors.Add(new AuthoringError(source, $"marker char '{marker.Symbol}' is already a ground char"));
            else if (MarkerChars.IsReserved(marker.Symbol))
                errors.Add(new AuthoringError(source, $"marker char '{marker.Symbol}' is reserved"));
            else if (!symbols.Add(marker.Symbol))
                errors.Add(new AuthoringError(source, $"duplicate marker char '{marker.Symbol}'"));
            if (string.IsNullOrWhiteSpace(marker.Kind))
                errors.Add(new AuthoringError(source, $"marker '{marker.Symbol}' has no kind"));
            else if (MarkerChars.IsBuiltInKind(marker.Kind))
                errors.Add(new AuthoringError(source, $"marker kind '{marker.Kind}' is built in"));
            else if (!kinds.Add(marker.Kind))
                errors.Add(new AuthoringError(source, $"duplicate marker kind '{marker.Kind}'"));
        }
    }
}
