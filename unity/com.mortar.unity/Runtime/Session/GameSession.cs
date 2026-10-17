using System.Collections.Generic;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using Mortar.Game.Simulation;
using Mortar.Json.Content;

namespace Mortar.Unity
{
    public sealed class GameSession
    {
        private readonly List<Command> _pending = new List<Command>();

        private GameSession(GameContent content, World world)
        {
            Content = content;
            World = world;
        }

        public GameContent Content { get; }

        public World World { get; }

        public static GameSession? NewGame(GameContent content, string classId, uint seed, out string? error) =>
            From(content, World.Create(content.Content, content.CreateLevelGenerator(), classId, seed), out error);

        public static GameSession? Load(GameContent content, SaveSlot slot, out string? error) =>
            From(content, slot.Read(content), out error);

        public void Enqueue(Command command) => _pending.Add(command);

        public IReadOnlyList<GameEvent> Tick()
        {
            var commands = _pending.ToArray();
            _pending.Clear();
            return World.Tick(commands);
        }

        private static GameSession? From(GameContent content, WorldResult result, out string? error)
        {
            error = result.Error;
            return result.World == null ? null : new GameSession(content, result.World);
        }
    }
}
