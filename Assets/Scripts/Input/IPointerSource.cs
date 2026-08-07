using UnityEngine;

public interface IPointerSource
{
    Vector2 ScreenPosition { get; }
    Vector2 WorldPosition { get; }
    bool IsPressed { get; }
}
