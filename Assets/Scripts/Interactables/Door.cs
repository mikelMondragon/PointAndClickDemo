using PointAndClickDemo.Characters.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PointAndClickDemo.Interactables
{
    /// <summary>Door that only opens once the key has been picked up. Reloads the scene.</summary>
    public class Door : MonoBehaviour, IInteractable
    {
        [SerializeField]
        [Tooltip("Maximum distance at which the player can open it")]
        private float interactionDistance = 2f;

        public bool CanBeInteracted(GameObject instigator)
        {
            if (Vector3.Distance(instigator.transform.position, transform.position) > interactionDistance)
                return false;

            return instigator.TryGetComponent(out Inventory inventory) && inventory.CanUseDoor;
        }

        public void Interact(GameObject instigator)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
