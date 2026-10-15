using System;
using System.Collections.Generic;
using Mortar.Game.Commands;
using Mortar.Game.Events;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class MortarGame : MonoBehaviour
    {
        private readonly TickClock _clock = new TickClock();

        public event Action<GameSession>? SessionStarted;

        public event Action<IReadOnlyList<GameEvent>>? Ticked;

        public event Action<string>? Notice;

        public GameSession? Session { get; private set; }

        public SaveSlot? Slot { get; set; }

        public bool Paused { get; set; }

        public float Alpha => _clock.Alpha;

        public void Begin(GameSession session)
        {
            Session = session;
            Paused = false;
            _clock.Reset();
            SessionStarted?.Invoke(session);
        }

        public void End()
        {
            Session = null;
            _clock.Reset();
        }

        public void Enqueue(Command command)
        {
            if (Session != null && !Paused)
            {
                Session.Enqueue(command);
            }
        }

        public void Step(float deltaSeconds)
        {
            if (Session == null || Paused)
            {
                return;
            }
            var ticks = _clock.Advance(deltaSeconds);
            for (var i = 0; i < ticks && Session != null && !Paused; i++)
            {
                RunTick();
            }
        }

        public void RunTick()
        {
            if (Session == null)
            {
                return;
            }
            var events = Session.Tick();
            if (Slot != null && ContainsLevelChange(events))
            {
                Notice?.Invoke(Save() ?? "Game saved");
            }
            Ticked?.Invoke(events);
        }

        public string? Save()
        {
            if (Session == null)
            {
                return "no game in progress";
            }
            return Slot == null ? "no save slot" : Slot.Write(Session.World);
        }

        private static bool ContainsLevelChange(IReadOnlyList<GameEvent> events)
        {
            foreach (var gameEvent in events)
            {
                if (gameEvent is LevelChanged)
                {
                    return true;
                }
            }
            return false;
        }

        private void Update() => Step(Time.deltaTime);
    }
}
