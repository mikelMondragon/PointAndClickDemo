using UnityEngine;

/// <summary>
/// Traduce el estado de <see cref="CharacterMovement"/> a la capa visual:
/// orienta el sprite y alimenta el parámetro "Speed" del Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class CharacterAnimator : MonoBehaviour
{
    static readonly int SpeedHash = Animator.StringToHash("Speed");

    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] CharacterMovement movement;
    [SerializeField, Tooltip("Si se deja vacío se busca en este mismo GameObject")]
    Animator animator;
    [SerializeField, Tooltip("Velocidad mínima para considerar que hay movimiento horizontal real")]
    float velocityThreshold = 0.01f;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (movement == null) movement = GetComponent<CharacterMovement>();
    }

    void Update()
    {
        float scale = Mathf.Max(0.0001f, transform.localScale.x);
        Vector2 velocity = movement.Velocity / scale;

        animator.SetFloat(SpeedHash, velocity.magnitude);

        if (Mathf.Abs(velocity.x) > velocityThreshold)
            spriteRenderer.flipX = velocity.x < 0f;
    }
}
