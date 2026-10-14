using Mortar.Core.Layouts;

namespace Mortar.Core.Generation;

public sealed class GenerationResult
{
    private GenerationResult(Layout? layout, IReadOnlyList<FailureCount> failures, int attempts)
    {
        Layout = layout;
        Failures = failures;
        Attempts = attempts;
    }

    public Layout? Layout { get; }

    public IReadOnlyList<FailureCount> Failures { get; }

    public int Attempts { get; }

    public bool Succeeded => Layout is not null;

    public string FailureSummary => string.Join(", ", Failures.Select(f => $"{f.Reason}: {f.Count}"));

    internal static GenerationResult Success(Layout layout, IReadOnlyList<FailureCount> earlierFailures) =>
        new GenerationResult(layout, earlierFailures, layout.Attempt + 1);

    internal static GenerationResult Failure(IReadOnlyList<FailureCount> failures, int attempts) =>
        new GenerationResult(null, failures, attempts);
}
