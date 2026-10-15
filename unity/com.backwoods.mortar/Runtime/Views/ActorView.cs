using Mortar.Game;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class ActorView : MonoBehaviour
    {
        public const float FlashSeconds = 0.15f;
        public const float FallSeconds = 0.3f;

        private static readonly Color HitColor = new Color(1f, 0.35f, 0.35f);
        private static readonly Color CorpseColor = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color ArrowColor = new Color(1f, 0.85f, 0.3f, 0.8f);

        private SpriteRenderer _body = null!;
        private SpriteRenderer _arrow = null!;
        private Vector3 _from;
        private Vector3 _to;
        private Vector3 _target;
        private Cell _cell;
        private ActionKind _action;
        private int _elapsed;
        private int _duration;
        private Direction _facing;
        private float _flashUntil = -1f;
        private float _diedAt = -1f;

        public int ActorId { get; private set; }

        public bool IsCorpse { get; private set; }

        public Color Tint => _body.color;

        public static ActorView Create(Transform parent, Actor actor, Sprite sprite, CellSpace space)
        {
            var body = SpriteObjects.Create(parent, $"Actor {actor.Id}", sprite, LevelView.WorldOrder);
            var view = body.gameObject.AddComponent<ActorView>();
            view._body = body;
            view.ActorId = actor.Id;
            view._arrow = SpriteObjects.Create(body.transform, "Facing", GeneratedSprites.Arrow, LevelView.FloorOrder + 2);
            view._arrow.color = ArrowColor;
            view._cell = actor.Cell;
            view._from = view._to = view._target = space.Center(actor.Cell);
            return view;
        }

        public void Sync(Actor actor, LevelState level, CellSpace space)
        {
            if (actor.Cell != _cell)
            {
                _from = space.Center(_cell);
                _cell = actor.Cell;
            }
            _to = space.Center(actor.Cell);
            if (actor.Action != ActionKind.Moving)
            {
                _from = _to;
            }
            _action = actor.Action;
            _elapsed = actor.ActionElapsed;
            _duration = actor.ActionDuration;
            _facing = actor.Facing;
            var target = actor.Action == ActionKind.Attacking ? level.Find(actor.ActionTargetId) : null;
            _target = target != null ? space.Center(target.Cell) : _to;
            if (actor.IsDead && !IsCorpse)
            {
                IsCorpse = true;
                _diedAt = Time.time;
            }
        }

        public void Render(float alpha, float now)
        {
            var progress = ActorPose.Progress(_elapsed, _duration, alpha);
            transform.position = ActorPose.Body(IsCorpse ? ActionKind.Idle : _action, progress, _from, _to, _target);
            _body.flipX = ActorPose.FacesLeft(_facing);
            _arrow.enabled = !IsCorpse;
            _arrow.transform.localRotation = Quaternion.Euler(0f, 0f, ActorPose.ArrowDegrees(_facing));
            if (IsCorpse)
            {
                var fall = Mathf.Clamp01((now - _diedAt) / FallSeconds);
                transform.rotation = Quaternion.Euler(0f, 0f, 90f * fall * (_body.flipX ? -1f : 1f));
                _body.color = CorpseColor;
                _body.sortingOrder = LevelView.FloorOrder + 3;
                return;
            }
            transform.rotation = Quaternion.identity;
            _body.color = now < _flashUntil ? HitColor : Color.white;
        }

        public void Flash(float now) => _flashUntil = now + FlashSeconds;
    }
}
