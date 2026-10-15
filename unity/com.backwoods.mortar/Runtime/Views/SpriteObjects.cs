using UnityEngine;

namespace Mortar.Unity
{
    public static class SpriteObjects
    {
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
            var vertices = sprite.vertices;
            var triangles = sprite.triangles;
            for (var i = 0; i + 2 < triangles.Length; i += 3)
            {
                if (InTriangle(spot, vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]))
                {
                    return true;
                }
            }
            return false;
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
