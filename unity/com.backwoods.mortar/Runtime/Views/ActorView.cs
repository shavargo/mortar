using Mortar.Game;
using Mortar.Game.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mortar.Unity
{
    public sealed class ActorView : MonoBehaviour
    {
        public const float FlashSeconds = 0.15f;
        public const float FallSeconds = 0.3f;
        public const float BodyScale = 0.8f;

        private static readonly Color HitColor = new Color(1f, 0.35f, 0.35f);
        private static readonly Color CorpseColor = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color ArrowColor = new Color(1f, 0.85f, 0.3f, 0.8f);

        private SortingGroup _sorting = null!;
        private SpriteRenderer _body = null!;
        private SpriteRenderer _arrow = null!;
        private ClipSet? _clips;
        private Vector3 _from;
        private Vector3 _to;
        private Vector3 _target;
        private Cell _cell;
        private ActionKind _action;
        private int _elapsed;
        private int _duration;
        private int _hitTick;
        private Direction _facing;
        private float _flashUntil = -1f;
        private float _hitAt = float.NegativeInfinity;
        private float _diedAt = -1f;

        public int ActorId { get; private set; }

        public bool IsCorpse { get; private set; }

        public bool IsBaked => _clips != null;

        public Vector3 GroundPosition { get; private set; }

        public Color Tint => _body.color;

        public SortingGroup Sorting => _sorting;

        public SpriteRenderer Body => _body;

        public static ActorView Create(Transform parent, Actor actor, Sprite sprite, ClipSet? clips, CellSpace space)
        {
            var go = new GameObject($"Actor {actor.Id}");
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<ActorView>();
            view._sorting = go.AddComponent<SortingGroup>();
            view._sorting.sortingOrder = LevelView.WorldOrder;
            view._clips = clips;
            view._body = SpriteObjects.Create(go.transform, "Body", sprite, 0);
            view._body.transform.localScale = Vector3.one * (clips != null ? 1f : BodyScale);
            view._arrow = SpriteObjects.Create(go.transform, "Facing", GeneratedSprites.Arrow, -1);
            view._arrow.color = ArrowColor;
            view.ActorId = actor.Id;
            view._cell = actor.Cell;
            view._from = view._to = view._target = space.Center(actor.Cell);
            view.GroundPosition = view._to;
            go.transform.position = view._to;
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
            _hitTick = actor.Profile.Timings.HitFrame;
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
            var action = IsCorpse ? ActionKind.Idle : _action;
            GroundPosition = ActorPose.Ground(action, progress, _from, _to);
            transform.position = GroundPosition;
            _arrow.enabled = !IsCorpse;
            _arrow.transform.localRotation = Quaternion.Euler(0f, 0f, ActorPose.ArrowDegrees(_facing));
            if (IsCorpse)
            {
                _sorting.sortingOrder = LevelView.FloorOrder + 3;
            }
            if (_clips != null)
            {
                RenderClip(_clips, alpha, now);
                return;
            }
            RenderStill(action, progress, now);
        }

        public void Flash(float now)
        {
            _flashUntil = now + FlashSeconds;
            _hitAt = now;
        }

        public bool Covers(Vector3 point) => !IsCorpse && SpriteObjects.Covers(_body, point);

        private void RenderClip(ClipSet clips, float alpha, float now)
        {
            var moment = new AnimMoment
            {
                Action = _action, Elapsed = _elapsed, Duration = _duration, HitTick = _hitTick,
                Alpha = alpha, Now = now, HitAt = _hitAt, DiedAt = _diedAt, IsDead = IsCorpse,
            };
            var frame = ActorAnimator.Pick(clips, moment);
            _body.sprite = clips.Get(frame.Name).Frame(_facing, frame.Index);
            _body.transform.localPosition = Vector3.zero;
            _body.color = !IsCorpse && now < _flashUntil ? HitColor : Color.white;
        }

        private void RenderStill(ActionKind action, float progress, float now)
        {
            _body.transform.localPosition = ActorPose.Body(action, progress, _from, _to, _target) - GroundPosition;
            _body.flipX = ActorPose.FacesLeft(_facing);
            if (IsCorpse)
            {
                var fall = Mathf.Clamp01((now - _diedAt) / FallSeconds);
                _body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f * fall * (_body.flipX ? -1f : 1f));
                _body.color = CorpseColor;
                return;
            }
            _body.transform.localRotation = Quaternion.identity;
            _body.color = now < _flashUntil ? HitColor : Color.white;
        }
    }
}
