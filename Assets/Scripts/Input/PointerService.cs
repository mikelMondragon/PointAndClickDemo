using UnityEngine;
using UnityEngine.InputSystem;

namespace PointAndClickDemo.Input
{
    /// <summary>
    /// Single point of truth for the pointer. Switches between mouse and gamepad
    /// cursor depending on the active control scheme.
    /// </summary>
    public class PointerService : MonoBehaviour
    {
        private const string GamepadSchemeName = "Gamepad";

        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private Camera cam;
        [SerializeField] private GamepadVirtualCursor gamepadCursor;

        private MousePointerSource mouseSource;
        private IPointerSource active;

        public static PointerService Instance { get; private set; }

        public IPointerSource Current => active;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("A PointerService already exists in the scene; this one is destroyed.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            mouseSource = new MousePointerSource(cam);
            active = mouseSource;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            if (playerInput != null) playerInput.onControlsChanged += HandleControlsChanged;
        }

        private void OnDisable()
        {
            if (playerInput != null) playerInput.onControlsChanged -= HandleControlsChanged;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            Instance = null;
            Cursor.visible = true;
        }

        private void HandleControlsChanged(PlayerInput input)
        {
            bool isGamepad = input.currentControlScheme == GamepadSchemeName;
            active = isGamepad ? gamepadCursor : mouseSource;
        }
    }
}
