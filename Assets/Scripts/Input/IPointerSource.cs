using UnityEngine;

namespace PointAndClickDemo.Input
{
    /// <summary>
    /// Abstract pointer source: mouse, gamepad virtual cursor, touch...
    /// </summary>
    public interface IPointerSource
    {
        Vector2 ScreenPosition { get; }

        Vector2 WorldPosition { get; }

        bool IsPressed { get; }
    }
}
