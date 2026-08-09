using System.Collections.Generic;
using PointAndClickDemo.Input;
using PointAndClickDemo.Interactables;
using PointAndClickDemo.Pathfinding;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PointAndClickDemo.Characters.Player
{
    /// <summary>
    /// Turns pointer clicks into either an interaction or a movement order.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Any MonoBehaviour implementing IPathProvider")]
        private MonoBehaviour pathProviderSource;

        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private CharacterMovement movement;

        private IPathProvider pathProvider;
        private readonly List<Vector2> pathBuffer = new();

        private void Awake()
        {
            pathProvider = ResolvePathProvider();
            if (pathProvider == null) enabled = false;
        }

        /// <summary>
        /// Uses the inspector reference when it is valid. Otherwise it looks the provider up
        /// in the scene: a Unity reimport can null out the serialized reference, and that
        /// should not take the player controller down with it.
        /// </summary>
        private IPathProvider ResolvePathProvider()
        {
            if (pathProviderSource is IPathProvider provider) return provider;

            if (pathProviderSource != null)
            {
                Debug.LogError($"{pathProviderSource.name} does not implement IPathProvider.", this);
                return null;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is not IPathProvider found) continue;

                pathProviderSource = behaviour;
                Debug.LogWarning(
                    $"Path Provider Source was empty; fell back to {behaviour.name}. " +
                    "Assign it in the inspector so this does not rely on a scene lookup.", this);
                return found;
            }

            Debug.LogError("No IPathProvider found in the scene.", this);
            return null;
        }

        private void Update()
        {
            if (PointerService.Instance == null) return;
            if (!PointerService.Instance.Current.IsPressed) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            ResolveClick(PointerService.Instance.Current.WorldPosition);
        }

        private void ResolveClick(Vector2 worldPoint)
        {
            // When the click resolves an interaction the character stays put.
            // When the target exists but is out of reach, we walk towards it.
            if (TryInteract(worldPoint)) return;

            MoveTo(worldPoint);
        }

        private bool TryInteract(Vector2 worldPoint)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPoint, interactableLayer);
            if (hit == null) return false;

            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null || !interactable.CanBeInteracted(gameObject)) return false;

            interactable.Interact(gameObject);
            return true;
        }

        private void MoveTo(Vector2 worldPoint)
        {
            // Unreachable destination: the click is ignored and the current path is kept.
            if (!pathProvider.TryGetPath(transform.position, worldPoint, pathBuffer)) return;

            movement.SetPath(pathBuffer);
            DrawDebugPath();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void DrawDebugPath()
        {
            Vector2 previous = transform.position;
            foreach (Vector2 point in pathBuffer)
            {
                Debug.DrawLine(previous, point, Color.yellow, 2f);
                previous = point;
            }
        }
    }
}
