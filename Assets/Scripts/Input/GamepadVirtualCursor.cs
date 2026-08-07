using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadVirtualCursor : MonoBehaviour, IPointerSource
{
    [SerializeField] Camera cam;
    [SerializeField] float speedPixelsPerSecond = 900f;
    [SerializeField, Tooltip("Ignora ruido del stick por debajo de este valor")]
    float deadzone = 0.15f;

    Vector2 screenPos;

    public Vector2 ScreenPosition => screenPos;
    public Vector2 WorldPosition => cam.ScreenToWorldPoint(screenPos);
    public bool IsPressed { get; private set; }

    void Awake()
    {
        // Arranca centrado en pantalla.
        screenPos = new Vector2(Screen.width, Screen.height) * 0.5f;
    }

    void Update()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) { IsPressed = false; return; }

        Vector2 stick = gamepad.leftStick.ReadValue();
        if (stick.magnitude < deadzone) stick = Vector2.zero;

        screenPos += stick * speedPixelsPerSecond * Time.deltaTime;
        screenPos.x = Mathf.Clamp(screenPos.x, 0f, Screen.width);
        screenPos.y = Mathf.Clamp(screenPos.y, 0f, Screen.height);

        IsPressed = gamepad.buttonSouth.wasPressedThisFrame;
    }
}