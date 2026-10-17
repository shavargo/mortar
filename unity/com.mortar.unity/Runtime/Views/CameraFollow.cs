using System;
using UnityEngine;

namespace Mortar.Unity
{
    [DefaultExecutionOrder(100)]
    public sealed class CameraFollow : MonoBehaviour
    {
        public const float Size = 5.5f;
        public const float Depth = -10f;

        private Func<Vector3?>? _target;

        public Camera Camera { get; private set; } = null!;

        public AsciiFilter Filter { get; private set; } = null!;

        public static CameraFollow Create(Transform parent)
        {
            var go = new GameObject("Camera");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 0f, Depth);
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transparencySortMode = TransparencySortMode.CustomAxis;
            camera.transparencySortAxis = Vector3.up;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            go.AddComponent<AudioListener>();
            var follow = go.AddComponent<CameraFollow>();
            follow.Camera = camera;
            follow.Filter = AsciiFilter.Attach(camera, AsciiResources.Load());
            return follow;
        }

        public void Follow(Func<Vector3?> target) => _target = target;

        public void Snap() => LateUpdate();

        private void LateUpdate()
        {
            var target = _target?.Invoke();
            if (target.HasValue)
            {
                transform.position = new Vector3(target.Value.x, target.Value.y, Depth);
            }
        }
    }
}
