using Mortar.Game.Simulation;

namespace Mortar.Unity
{
    public struct AnimMoment
    {
        public ActionKind Action;
        public int Elapsed;
        public int Duration;
        public int HitTick;
        public float Alpha;
        public float Now;
        public float HitAt;
        public float DiedAt;
        public bool IsDead;
    }
}
