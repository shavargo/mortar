using Mortar.Core.Model;
using Mortar.Core.Rng;
using Mortar.Game.Commands;
using Mortar.Game.Content;
using Mortar.Game.Events;
using Mortar.Game.Items;
using Mortar.Game.Saves;

namespace Mortar.Game.Simulation;

public sealed class World
{
    private readonly Dictionary<int, LevelState> _levels = new Dictionary<int, LevelState>();

    private World(ContentSet content, ILevelGenerator generator, uint seed, ClassDefinition playerClass, PlayerProgress progress, Pcg32 rng)
    {
        Content = content;
        Generator = generator;
        Seed = seed;
        PlayerClass = playerClass;
        Rng = rng;
        Progress = progress;
        var profile = PlayerStats.Compute(playerClass, Progress, content.Rules, GearBonus.None);
        Player = new Actor(Actor.PlayerId, ActorKind.Player, playerClass.Id, profile);
    }

    public ContentSet Content { get; }

    public uint Seed { get; }

    public long TickCount { get; internal set; }

    public int Depth { get; internal set; }

    public ClassDefinition PlayerClass { get; }

    public PlayerProgress Progress { get; }

    public Actor Player { get; }

    public Belongings Belongings { get; } = new Belongings();

    public LevelState Level => _levels[Depth];

    public bool IsGameOver => Player.IsDead;

    public IReadOnlyList<int> VisitedDepths => _levels.Keys.OrderBy(depth => depth).ToList();

    internal Pcg32 Rng { get; }

    internal ILevelGenerator Generator { get; }

    internal int NextActorId { get; set; } = Actor.PlayerId + 1;

    internal int NextItemId { get; set; } = 1;

    internal int NextProjectileId { get; set; } = 1;

    public static WorldResult Create(ContentSet content, ILevelGenerator generator, string classId, uint seed)
    {
        if (!content.TryGetClass(classId, out var playerClass))
        {
            return WorldResult.Failure($"unknown class '{classId}'");
        }
        var world = new World(content, generator, seed, playerClass, PlayerProgress.Start(playerClass), Pcg32.ForAttempt("world", seed, 0));
        StartingKit.Give(world);
        world.RefreshPlayer();
        world.Player.RestoreVitals();
        var error = LevelEntry.Enter(world, content.ShallowestLevel, MarkerKinds.Entrance);
        return error is null ? WorldResult.Success(world) : WorldResult.Failure(error);
    }

    public static WorldResult Restore(WorldSnapshot snapshot, ContentSet content, ILevelGenerator generator) =>
        WorldRestorer.Restore(snapshot, content, generator);

    internal static World ForRestore(
        ContentSet content, ILevelGenerator generator, uint seed, ClassDefinition playerClass, PlayerProgress progress, Pcg32 rng) =>
        new World(content, generator, seed, playerClass, progress, rng);

    public LevelState GetLevel(int depth) => _levels[depth];

    public IReadOnlyList<GameEvent> Tick(IReadOnlyList<Command> commands) => TickRunner.Run(this, commands);

    internal LevelState? FindLevel(int depth) => _levels.TryGetValue(depth, out var level) ? level : null;

    internal void AddLevel(LevelState level) => _levels.Add(level.Depth, level);

    internal void RefreshPlayer() =>
        Player.ApplyProfile(PlayerStats.Compute(PlayerClass, Progress, Content.Rules, GearBonus.From(Belongings.Equipment)));

    internal Actor SpawnMonster(LevelState level, MonsterDefinition monster, Cell cell)
    {
        var maxLife = Rng.NextInt(monster.Life.Min, monster.Life.Max + 1);
        var actor = new Actor(NextActorId++, ActorKind.Monster, monster.Id, CombatProfile.ForMonster(monster, maxLife));
        level.Add(actor, cell);
        return actor;
    }
}
