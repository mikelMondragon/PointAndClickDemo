using UnityEngine;
using UnityEngine.InputSystem;

namespace PointAndClickDemo.Input
{
    /// <summary>Pointer backed by the system mouse.</summary>
    public class MousePointerSource : IPointerSource
    {
        private readonly Camera cam;

        public MousePointerSource(Camera cam) => this.cam = cam;

        public Vector2 ScreenPosition => Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        public Vector2 WorldPosition => cam.ScreenToWorldPoint(ScreenPosition);

        public bool IsPressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }
}
