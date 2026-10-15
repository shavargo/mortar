using Mortar.Game;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class PropAnimator : MonoBehaviour
    {
        private SpriteRenderer _renderer = null!;
        private ActorClip _clip = null!;

        public ActorClip Clip => _clip;

        public static PropAnimator Attach(SpriteRenderer renderer, ActorClip clip)
        {
            var animator = renderer.gameObject.AddComponent<PropAnimator>();
            animator._renderer = renderer;
            animator._clip = clip;
            animator.Show(Time.time);
            return animator;
        }

        public void Show(float now) =>
            _renderer.sprite = _clip.Frame(Direction.South, ActorAnimator.LoopFrame(now, _clip.Fps, _clip.Frames));

        private void Update() => Show(Time.time);
    }
}
