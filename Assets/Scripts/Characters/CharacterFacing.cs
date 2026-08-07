using UnityEngine;

public class CharacterFacing : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] CharacterMovement movement;
    [SerializeField, Tooltip("Velocidad mínima para considerar que hay movimiento horizontal real")]
    float velocityThreshold = 0.01f;

    void Update()
    {
        float vx = movement.Velocity.x;
        if (Mathf.Abs(vx) > velocityThreshold)
            spriteRenderer.flipX = vx < 0f;
    }
}