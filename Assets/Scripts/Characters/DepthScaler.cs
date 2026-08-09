using UnityEngine;

namespace PointAndClickDemo.Characters
{
    /// <summary>
    /// Scales the character by depth: the lower it stands in the scene,
    /// the closer to the camera and the bigger it gets.
    /// </summary>
    public class DepthScaler : MonoBehaviour
    {
        [Header("Y references (world units)")]
        [SerializeField]
        [Tooltip("Y at the back of the scene (farthest, smallest character)")]
        private float yFar = 2f;

        [SerializeField]
        [Tooltip("Y at the front (closest, biggest character)")]
        private float yNear = -3f;

        [Header("Scales")]
        [SerializeField] private float scaleFar = 0.5f;
        [SerializeField] private float scaleNear = 1.2f;

        /// <summary>Scale applied this frame. Movement and animation consume it.</summary>
        public float CurrentScale { get; private set; } = 1f;

        private void LateUpdate()
        {
            float depth = Mathf.InverseLerp(yFar, yNear, transform.position.y);
            CurrentScale = Mathf.Lerp(scaleFar, scaleNear, depth);
            transform.localScale = new Vector3(CurrentScale, CurrentScale, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(-50f, yFar), new Vector3(50f, yFar));

            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(-50f, yNear), new Vector3(50f, yNear));
        }
    }
}
