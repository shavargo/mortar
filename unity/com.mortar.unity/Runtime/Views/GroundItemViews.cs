using System.Collections.Generic;
using Mortar.Game.Simulation;
using UnityEngine;

namespace Mortar.Unity
{
    public sealed class GroundItemViews : MonoBehaviour
    {
        public const string GoldIcon = "gold";
        public const float IconScale = 0.5f;

        private readonly Dictionary<int, SpriteRenderer> _views = new Dictionary<int, SpriteRenderer>();
        private readonly HashSet<int> _seen = new HashSet<int>();
        private readonly List<int> _stale = new List<int>();

        public int Count => _views.Count;

        public static GroundItemViews Create(Transform parent)
        {
            var go = new GameObject("Ground Items");
            go.transform.SetParent(parent, false);
            return go.AddComponent<GroundItemViews>();
        }

        public void Sync(LevelState level, CellSpace space, SpriteCatalog catalog)
        {
            _seen.Clear();
            foreach (var ground in level.GroundItems)
            {
                _seen.Add(ground.Id);
                if (_views.ContainsKey(ground.Id))
                {
                    continue;
                }
                var icon = ground.Item != null ? ground.Item.Definition.IconId : GoldIcon;
                var view = SpriteObjects.Create(transform, $"Ground {ground.Id}", catalog.WorldIcon(icon), LevelView.FloorOrder + 1);
                view.transform.position = space.Center(ground.Cell);
                view.transform.localScale = Vector3.one * IconScale;
                var glow = MaskMaterials.Glow;
                if (glow != null)
                {
                    view.sharedMaterial = glow;
                }
                _views.Add(ground.Id, view);
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

        public void Clear()
        {
            foreach (var view in _views.Values)
            {
                Destroy(view.gameObject);
            }
            _views.Clear();
        }
    }
}
