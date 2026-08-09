using System.Collections.Generic;
using UnityEngine;

namespace PointAndClickDemo.Characters
{
    /// <summary>
    /// Walks an already computed path waypoint by waypoint.
    /// It does not search for paths: that is the pathfinder's job.
    /// </summary>
    public class CharacterMovement : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Units per second at scale 1")]
        private float speed = 3f;

        [SerializeField]
        [Tooltip("Distance at which a waypoint counts as reached")]
        private float arriveThreshold = 0.05f;

        private readonly List<Vector2> path = new();
        private int index;

        public bool IsMoving => index < path.Count;

        public Vector2 Velocity { get; private set; }

        public void SetPath(IReadOnlyList<Vector2> newPath)
        {
            path.Clear();
            for (int i = 0; i < newPath.Count; i++) path.Add(newPath[i]);
            index = 0;
        }

        public void Stop()
        {
            path.Clear();
            index = 0;
            Velocity = Vector2.zero;
        }

        private void Update()
        {
            if (!IsMoving)
            {
                Velocity = Vector2.zero;
                return;
            }

            Vector2 current = transform.position;
            Vector2 target = path[index];

            // The current depth scale modulates the speed: far away = small = slower.
            float step = speed * transform.localScale.x * Time.deltaTime;
            Vector2 next = Vector2.MoveTowards(current, target, step);

            Velocity = Time.deltaTime > 0f ? (next - current) / Time.deltaTime : Vector2.zero;
            transform.position = new Vector3(next.x, next.y, transform.position.z);

            if (Vector2.Distance(next, target) <= arriveThreshold) index++;
        }
    }
}
