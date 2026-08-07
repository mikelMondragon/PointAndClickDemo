using UnityEngine;
using UnityEngine.InputSystem;

public class MousePointerSource : IPointerSource
{
    readonly Camera cam;

    public MousePointerSource(Camera cam) => this.cam = cam;

    public Vector2 ScreenPosition => Mouse.current.position.ReadValue();
    public Vector2 WorldPosition => cam.ScreenToWorldPoint(ScreenPosition);

    public bool IsPressed
        => Mouse.current.leftButton.wasPressedThisFrame;
}