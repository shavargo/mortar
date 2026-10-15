using System.Collections.Generic;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class ActorViews : MonoBehaviour
    {
        private readonly Dictionary<int, ActorView> _views = new Dictionary<int, ActorView>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _stale = new List<int>();
        private MortarGame? _game;

        public int Count => _views.Count;

        public static ActorViews Create(Transform parent, MortarGame? game)
        {
            var go = new GameObject("Actors");
            go.transform.SetParent(parent, false);
            var views = go.AddComponent<ActorViews>();
            views._game = game;
            return views;
        }

        public ActorView? Find(int actorId) => _views.TryGetValue(actorId, out var view) ? view : null;

        public void Sync(World world, CellSpace space, SpriteCatalog catalog)
        {
            var level = world.Level;
            _seen.Clear();
            foreach (var actor in level.Actors)
            {
                if (!_views.TryGetValue(actor.Id, out var view))
                {
                    view = CreateView(world, actor, space, catalog);
                    _views.Add(actor.Id, view);
                }
                view.Sync(actor, level, space);
                _seen.Add(actor.Id);
            }
            _stale.Clear();
            foreach (var id in _views.Keys)
            {
                if (!_seen.Contains(id))
                {
                    _stale.Add(id);
                }
            }
            foreach (var id in _stale)
            {
                Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
        }

        private ActorView CreateView(World world, Actor actor, CellSpace space, SpriteCatalog catalog)
        {
            var spriteSet = SpriteSetOf(world, actor);
            var clips = ClipSet.For(catalog, spriteSet);
            var sprite = clips != null ? clips.Get(ClipNames.Idle).Frame(actor.Facing, 0) : catalog.Actor(spriteSet);
            return ActorView.Create(transform, actor, sprite, clips, space);
        }

        public void Clear()
        {
            foreach (var view in _views.Values)
            {
                Destroy(view.gameObject);
            }
            _views.Clear();
        }

        public void Flash(int actorId)
        {
            var view = Find(actorId);
            if (view != null)
            {
                view.Flash(Time.time);
            }
        }

        public void RenderNow() => LateUpdate();

        public int LivingMonsterAt(World world, Vector3 point, Vector3 sortAxis)
        {
            ActorView? picked = null;
            foreach (var view in _views.Values)
            {
                var actor = world.Level.Find(view.ActorId);
                if (actor == null || actor.Kind != ActorKind.Monster || !actor.IsAlive || !view.Covers(point))
                {
                    continue;
                }
                if (picked == null || IsNearer(view, picked, sortAxis))
                {
                    picked = view;
                }
            }
            return picked != null ? picked.ActorId : 0;
        }

        private static bool IsNearer(ActorView view, ActorView other, Vector3 sortAxis)
        {
            var depth = Vector3.Dot(view.GroundPosition, sortAxis);
            var otherDepth = Vector3.Dot(other.GroundPosition, sortAxis);
            if (depth != otherDepth)
            {
                return depth < otherDepth;
            }
            if (view.GroundPosition.x != other.GroundPosition.x)
            {
                return view.GroundPosition.x < other.GroundPosition.x;
            }
            return view.ActorId < other.ActorId;
        }

        public static string SpriteSetOf(World world, Actor actor)
        {
            if (actor.Kind == ActorKind.Player)
            {
                return world.PlayerClass.SpriteSet;
            }
            return world.Content.TryGetMonster(actor.DefinitionId, out var monster) ? monster.SpriteSet : actor.DefinitionId;
        }

        private void LateUpdate()
        {
            var alpha = _game != null ? _game.Alpha : 0f;
            var now = Time.time;
            foreach (var view in _views.Values)
            {
                view.Render(alpha, now);
            }
        }
    }
}
