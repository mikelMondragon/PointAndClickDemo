using PointAndClickDemo.Characters.Player;
using UnityEngine;

namespace PointAndClickDemo.Interactables
{
    /// <summary>Pickup that unlocks the door.</summary>
    public class Collectable : MonoBehaviour, IInteractable
    {
        [SerializeField]
        [Tooltip("Maximum distance at which the player can pick it up")]
        private float interactionDistance = 2f;

        public bool CanBeInteracted(GameObject instigator)
        {
            return Vector3.Distance(instigator.transform.position, transform.position) <= interactionDistance;
        }

        public void Interact(GameObject instigator)
        {
            if (!instigator.TryGetComponent(out Inventory inventory))
            {
                Debug.LogWarning($"{instigator.name} has no Inventory: it cannot pick anything up.", this);
                return;
            }

            inventory.CanUseDoor = true;
            Destroy(gameObject);
        }
    }
}
