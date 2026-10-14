using Mortar.Core.Layouts;

namespace Mortar.Game.Simulation;

public sealed class LevelBuildResult
{
    private LevelBuildResult(Layout? layout, string? error)
    {
        Layout = layout;
        Error = error;
    }

    public Layout? Layout { get; }

    public string? Error { get; }

    public bool Succeeded => Layout is not null;

    public static LevelBuildResult Success(Layout layout) => new LevelBuildResult(layout, null);

    public static LevelBuildResult Failure(string error) => new LevelBuildResult(null, error);
}
