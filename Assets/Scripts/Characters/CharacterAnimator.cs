using UnityEngine;

namespace PointAndClickDemo.Characters
{
    /// <summary>
    /// Translates <see cref="CharacterMovement"/> state into the visual layer:
    /// faces the sprite and drives the Animator's "Speed" parameter.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimator : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CharacterMovement movement;

        [SerializeField]
        [Tooltip("Left empty, it is looked up on this same GameObject")]
        private Animator animator;

        [SerializeField]
        [Tooltip("Minimum speed to count as actual horizontal movement")]
        private float velocityThreshold = 0.01f;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (movement == null) movement = GetComponent<CharacterMovement>();
        }

        private void Update()
        {
            // DepthScaler scales the transform and CharacterMovement multiplies the velocity
            // by that scale. Normalising keeps the threshold independent of depth.
            float scale = Mathf.Max(0.0001f, transform.localScale.x);
            Vector2 velocity = movement.Velocity / scale;

            animator.SetFloat(SpeedHash, velocity.magnitude);

            if (Mathf.Abs(velocity.x) > velocityThreshold)
                spriteRenderer.flipX = velocity.x < 0f;
        }
    }
}
