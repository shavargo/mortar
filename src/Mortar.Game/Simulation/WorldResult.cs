namespace Mortar.Game.Simulation;

public sealed class WorldResult
{
    private WorldResult(World? world, string? error)
    {
        World = world;
        Error = error;
    }

    public World? World { get; }

    public string? Error { get; }

    public bool Succeeded => World is not null;

    internal static WorldResult Success(World world) => new WorldResult(world, null);

    internal static WorldResult Failure(string error) => new WorldResult(null, error);
}
