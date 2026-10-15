using System.Collections.Generic;
using UnityEngine;

namespace Mortar.Unity
{
    public static class SpriteObjects
    {
        private static readonly Dictionary<Sprite, Vector2[]> Outlines = new Dictionary<Sprite, Vector2[]>();

        public static SpriteRenderer Create(Transform parent, string name, Sprite sprite, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            return renderer;
        }

        public static bool Covers(SpriteRenderer renderer, Vector3 point)
        {
            var sprite = renderer.sprite;
            var bounds = renderer.bounds;
            if (sprite == null || !renderer.enabled
                || point.x < bounds.min.x || point.x > bounds.max.x || point.y < bounds.min.y || point.y > bounds.max.y)
            {
                return false;
            }
            var transform = renderer.transform;
            var local = transform.InverseTransformPoint(new Vector3(point.x, point.y, transform.position.z));
            var spot = new Vector2(renderer.flipX ? -local.x : local.x, renderer.flipY ? -local.y : local.y);
            var corners = CornersOf(sprite);
            for (var i = 0; i + 2 < corners.Length; i += 3)
            {
                if (InTriangle(spot, corners[i], corners[i + 1], corners[i + 2]))
                {
                    return true;
                }
            }
            return false;
        }

        private static Vector2[] CornersOf(Sprite sprite)
        {
            if (!Outlines.TryGetValue(sprite, out var corners))
            {
                var vertices = sprite.vertices;
                var triangles = sprite.triangles;
                corners = new Vector2[triangles.Length];
                for (var i = 0; i < triangles.Length; i++)
                {
                    corners[i] = vertices[triangles[i]];
                }
                Outlines.Add(sprite, corners);
            }
            return corners;
        }

        private static bool InTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            var ab = Side(point, a, b);
            var bc = Side(point, b, c);
            var ca = Side(point, c, a);
            var anyNegative = ab < 0f || bc < 0f || ca < 0f;
            var anyPositive = ab > 0f || bc > 0f || ca > 0f;
            return !(anyNegative && anyPositive);
        }

        private static float Side(Vector2 point, Vector2 from, Vector2 to) =>
            (to.x - from.x) * (point.y - from.y) - (to.y - from.y) * (point.x - from.x);
    }
}
