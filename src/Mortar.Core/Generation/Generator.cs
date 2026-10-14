using Mortar.Core.Indoor;
using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Core.Validation;

namespace Mortar.Core.Generation;

public static class Generator
{
    public static GenerationResult Generate(CompiledArea area, uint seed)
    {
        var definition = area.Definition;
        var failures = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var attempt = 0; attempt < definition.MaxAttempts; attempt++)
        {
            var rng = Pcg32.ForAttempt(definition.Id, seed, attempt);
            var outcome = IndoorBuilder.Build(area, rng);
            var failure = outcome.Failure
                ?? LayoutValidator.Validate(outcome.Grid!, area.Tileset, outcome.Markers, definition.TargetSize);
            if (failure is null)
            {
                var layout = LayoutFactory.Create(area, seed, attempt, outcome.Grid!, outcome.Markers, rng);
                return GenerationResult.Success(layout, Summarize(failures));
            }
            failures[failure] = failures.TryGetValue(failure, out var count) ? count + 1 : 1;
        }
        return GenerationResult.Failure(Summarize(failures), definition.MaxAttempts);
    }

    private static IReadOnlyList<FailureCount> Summarize(Dictionary<string, int> failures) =>
        failures
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new FailureCount(pair.Key, pair.Value))
            .ToList();
}
