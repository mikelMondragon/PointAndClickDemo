using UnityEngine;

namespace PointAndClickDemo.Cameras
{
    public enum CameraFramingMode
    {
        /// <summary>The whole background fits on screen. Bars appear when the aspect does not match.</summary>
        Contain = 0,

        /// <summary>The background covers the screen; the excess is cropped and the camera follows the player.</summary>
        CoverFollow = 1,
    }

    /// <summary>
    /// Framing math, free of scene dependencies so it can be tested on its own.
    /// </summary>
    public static class CameraFraming
    {
        /// <summary>
        /// Orthographic size (half of the visible height) needed to frame the background.
        /// Contain takes the larger of the two fits (everything fits, screen space is left over);
        /// Cover takes the smaller one (the screen is filled, background is left over).
        /// </summary>
        public static float OrthographicSize(CameraFramingMode mode, Bounds background, float aspect)
        {
            if (aspect <= 0f) aspect = 1f;

            float fitToHeight = background.extents.y;
            float fitToWidth = background.extents.x / aspect;

            return mode == CameraFramingMode.Contain
                ? Mathf.Max(fitToHeight, fitToWidth)
                : Mathf.Min(fitToHeight, fitToWidth);
        }
    }
}
