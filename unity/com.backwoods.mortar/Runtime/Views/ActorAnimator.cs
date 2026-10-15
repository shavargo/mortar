using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public static class ActorAnimator
    {
        public static ClipFrame Pick(ClipSet clips, AnimMoment moment)
        {
            if (moment.IsDead)
            {
                var death = clips.Get(ClipNames.Death);
                return new ClipFrame(ClipNames.Death, OnceFrame(moment.Now - moment.DiedAt, death.Fps, death.Frames));
            }
            if (moment.Action == ActionKind.Attacking)
            {
                var attack = clips.Get(ClipNames.Attack);
                var ticks = moment.Elapsed + moment.Alpha;
                return new ClipFrame(ClipNames.Attack, AttackFrame(ticks, moment.Duration, moment.HitTick, attack.Frames, attack.HitFrame));
            }
            var progress = ActorPose.Progress(moment.Elapsed, moment.Duration, moment.Alpha);
            if (moment.Action == ActionKind.Recovering)
            {
                return new ClipFrame(ClipNames.Hit, ProgressFrame(progress, clips.Get(ClipNames.Hit).Frames));
            }
            var sinceHit = moment.Now - moment.HitAt;
            if (clips.Has(ClipNames.Hit) && sinceHit >= 0f && sinceHit < clips.Get(ClipNames.Hit).Seconds)
            {
                var hit = clips.Get(ClipNames.Hit);
                return new ClipFrame(ClipNames.Hit, OnceFrame(sinceHit, hit.Fps, hit.Frames));
            }
            if (moment.Action == ActionKind.Moving)
            {
                return new ClipFrame(ClipNames.Walk, ProgressFrame(progress, clips.Get(ClipNames.Walk).Frames));
            }
            var idle = clips.Get(ClipNames.Idle);
            return new ClipFrame(ClipNames.Idle, LoopFrame(moment.Now, idle.Fps, idle.Frames));
        }

        public static int LoopFrame(float seconds, float fps, int frames) =>
            frames <= 1 ? 0 : Mathf.FloorToInt(Mathf.Max(0f, seconds) * fps) % frames;

        public static int OnceFrame(float seconds, float fps, int frames) =>
            Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, seconds) * fps), 0, frames - 1);

        public static int ProgressFrame(float progress, int frames) =>
            Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress) * frames), 0, frames - 1);

        public static int AttackFrame(float ticks, int attackTicks, int hitTick, int frames, int hitFrame)
        {
            if (frames <= 1 || attackTicks <= 0)
            {
                return 0;
            }
            if (hitTick <= 0 || hitTick >= attackTicks)
            {
                return ProgressFrame(ticks / attackTicks, frames);
            }
            var hit = Mathf.Clamp(hitFrame, 0, frames - 1);
            var position = ticks <= hitTick
                ? ticks / hitTick * hit
                : hit + (ticks - hitTick) / (attackTicks - hitTick) * (frames - 1 - hit);
            return Mathf.Clamp(Mathf.FloorToInt(position + 1e-4f), 0, frames - 1);
        }
    }
}
