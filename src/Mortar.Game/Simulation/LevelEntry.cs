namespace Mortar.Game.Simulation;

internal static class LevelEntry
{
    public const int ArrivalSearchRadius = 3;

    public static string? Enter(World world, int depth, string arrivalMarker)
    {
        var level = world.FindLevel(depth);
        if (level is null)
        {
            level = Build(world, depth, out var buildError);
            if (level is null) return buildError;
        }
        var anchor = level.FindMarker(arrivalMarker);
        if (anchor is null)
        {
            return $"level {depth} has no {arrivalMarker} marker";
        }
        var cell = level.NearestFree(anchor.Value, ArrivalSearchRadius);
        if (cell is null)
        {
            return $"level {depth} has no free cell near its {arrivalMarker}";
        }
        if (world.Depth != 0)
        {
            world.Level.Remove(world.Player);
        }
        world.Player.ClearAction();
        world.Player.ClearIntent();
        level.Add(world.Player, cell.Value);
        world.Depth = depth;
        return null;
    }

    private static LevelState? Build(World world, int depth, out string? error)
    {
        var definition = world.Content.Level(depth);
        var seed = LevelSeeds.For(world.Seed, depth);
        var result = world.Generator.Generate(definition, seed);
        if (!result.Succeeded)
        {
            error = $"level {depth} could not be generated: {result.Error}";
            return null;
        }
        var level = new LevelState(depth, seed, result.Layout!);
        world.AddLevel(level);
        error = null;
        return level;
    }
}
