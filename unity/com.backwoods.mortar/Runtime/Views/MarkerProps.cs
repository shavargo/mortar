using System.Collections.Generic;
using Mortar.Core.Model;
using Mortar.Game;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public static class MarkerProps
    {
        public static string? SpriteFor(string markerKind, bool hasLevelAbove)
        {
            switch (markerKind)
            {
                case MarkerKinds.Exit:
                    return "stairs-down";
                case MarkerKinds.Entrance:
                    return hasLevelAbove ? "stairs-up" : null;
                case "smith":
                case "healer":
                case "sage":
                    return markerKind;
                default:
                    return null;
            }
        }

        public static void Place(
            LevelState level, CellSpace space, SpriteCatalog catalog, Transform parent, bool hasLevelAbove, List<GameObject> into)
        {
            foreach (var marker in level.Layout.Markers)
            {
                var spriteId = SpriteFor(marker.Kind, hasLevelAbove);
                if (spriteId == null)
                {
                    continue;
                }
                var prop = SpriteObjects.Create(parent, $"Prop {marker.Kind}", catalog.Actor(spriteId), LevelView.WorldOrder);
                prop.transform.position = space.Center(new Cell(marker.X, marker.Y));
                into.Add(prop.gameObject);
            }
        }
    }
}
