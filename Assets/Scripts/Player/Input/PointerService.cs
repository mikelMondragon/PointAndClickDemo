using UnityEngine;
using UnityEngine.InputSystem;

public class PointerService : MonoBehaviour
{
    public static PointerService Instance { get; private set; }

    [SerializeField] PlayerInput playerInput;
    [SerializeField] Camera cam;
    [SerializeField] GamepadVirtualCursor gamepadCursor;

    MousePointerSource mouseSource;
    IPointerSource active;

    public IPointerSource Current => active;

    void Awake()
    {
        Instance = this;
        mouseSource = new MousePointerSource(cam);
        active = mouseSource;
        Cursor.visible = false;
    }

    void OnEnable() => playerInput.onControlsChanged += HandleControlsChanged;
    void OnDisable() => playerInput.onControlsChanged -= HandleControlsChanged;

    void HandleControlsChanged(PlayerInput input)
    {
        bool isGamepad = input.currentControlScheme == "Gamepad";
        active = isGamepad ? gamepadCursor : mouseSource;
        Debug.Log($"Control scheme changed to: {input.currentControlScheme}");
    }


}