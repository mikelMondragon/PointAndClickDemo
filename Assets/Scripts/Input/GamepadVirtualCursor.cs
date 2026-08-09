using UnityEngine;
using UnityEngine.InputSystem;

namespace PointAndClickDemo.Input
{
    /// <summary>Virtual cursor driven by the gamepad's left stick.</summary>
    public class GamepadVirtualCursor : MonoBehaviour, IPointerSource
    {
        [SerializeField] private Camera cam;
        [SerializeField] private float speedPixelsPerSecond = 900f;

        [SerializeField]
        [Tooltip("Ignores stick noise below this value")]
        private float deadzone = 0.15f;

        private Vector2 screenPosition;

        public Vector2 ScreenPosition => screenPosition;

        public Vector2 WorldPosition => cam.ScreenToWorldPoint(screenPosition);

        // Read from the device on every access, same as MousePointerSource, so this does not
        // depend on the execution order between this Update and whoever polls it.
        public bool IsPressed => Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        private void Awake()
        {
            // Starts centred on screen.
            screenPosition = new Vector2(Screen.width, Screen.height) * 0.5f;
        }

        private void Update()
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null) return;

            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude < deadzone) stick = Vector2.zero;

            screenPosition += stick * (speedPixelsPerSecond * Time.deltaTime);
            screenPosition.x = Mathf.Clamp(screenPosition.x, 0f, Screen.width);
            screenPosition.y = Mathf.Clamp(screenPosition.y, 0f, Screen.height);
        }
    }
}
