using System;
using System.Collections.Generic;
using Mortar.Game.Events;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class SessionPresenter
    {
        private static readonly Color PlayerHurtColor = new Color(1f, 0.3f, 0.25f);
        private static readonly Color MissColor = new Color(0.7f, 0.7f, 0.7f);

        private readonly MortarApp _app;
        private readonly SpriteCatalog _catalog;
        private bool _deathShown;

        public SessionPresenter(MortarApp app, SpriteCatalog catalog)
        {
            _app = app;
            _catalog = catalog;
            Popups = new FloatingTexts(app.Ui.Overlay);
            app.Game.SessionStarted += OnSessionStarted;
            app.Game.Ticked += OnTicked;
            app.Game.Notice += text => Messages.Add(text, Time.time);
        }

        public event Action? PlayerDied;

        public MessageLog Messages { get; } = new MessageLog();

        public FloatingTexts Popups { get; }

        public void Reset()
        {
            _deathShown = false;
            Messages.Clear();
            Popups.Clear();
            _app.Level.Clear();
            _app.Actors.Clear();
            _app.Items.Clear();
        }

        private void OnSessionStarted(GameSession session)
        {
            Reset();
            ShowLevel(session);
        }

        private void OnTicked(IReadOnlyList<GameEvent> events)
        {
            var session = _app.Game.Session;
            if (session == null)
            {
                return;
            }
            foreach (var gameEvent in events)
            {
                if (gameEvent is LevelChanged)
                {
                    ShowLevel(session);
                }
                Route(gameEvent);
            }
            SyncViews(session);
            if (session.World.IsGameOver && !_deathShown)
            {
                _deathShown = true;
                PlayerDied?.Invoke();
            }
        }

        private void ShowLevel(GameSession session)
        {
            var world = session.World;
            Popups.Clear();
            _app.Actors.Clear();
            _app.Items.Clear();
            _app.Level.Show(world.Level, LevelView.TilesetFor(session.Content, world.Level), world.Depth > world.Content.ShallowestLevel);
            SyncViews(session);
            _app.Actors.RenderNow();
            _app.CameraRig.Snap();
        }

        private void SyncViews(GameSession session)
        {
            _app.Actors.Sync(session.World, _app.Level.Space, _catalog);
            _app.Items.Sync(session.World.Level, _app.Level.Space, _catalog);
        }

        private void Route(GameEvent gameEvent)
        {
            var text = EventText.Describe(gameEvent);
            if (text != null)
            {
                Messages.Add(text, Time.time);
            }
            var popup = CombatFeedback.For(gameEvent);
            if (!popup.HasValue)
            {
                return;
            }
            var view = _app.Actors.Find(popup.Value.ActorId);
            if (view == null)
            {
                return;
            }
            Popups.Spawn(popup.Value.Text, view.transform.position, ColorFor(popup.Value), Time.time);
            if (popup.Value.IsHit)
            {
                _app.Actors.Flash(popup.Value.ActorId);
            }
        }

        private static Color ColorFor(Popup popup)
        {
            if (!popup.IsHit) return MissColor;
            return popup.HurtsPlayer ? PlayerHurtColor : Color.white;
        }
    }
}
