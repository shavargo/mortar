using System.Collections.Generic;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public static class GrassTrampers
    {
        public static void Collect(
            List<Vector3> into, World? world, ActorViews? actors, LevelView? level)
        {
            into.Clear();
            if (world == null || actors == null)
            {
                return;
            }
            foreach (var actor in world.Level.Actors)
            {
                if (!actor.IsAlive)
                {
                    continue;
                }
                var view = actors.Find(actor.Id);
                if (view != null)
                {
                    into.Add(view.GroundPosition);
                }
            }
            if (level == null)
            {
                return;
            }
            foreach (var prop in level.Props)
            {
                if (IsTownNpc(prop.name))
                {
                    into.Add(prop.transform.position);
                }
            }
        }

        public static bool IsTownNpc(string name) =>
            name == "Prop smith" || name == "Prop healer" || name == "Prop sage";
    }
}
