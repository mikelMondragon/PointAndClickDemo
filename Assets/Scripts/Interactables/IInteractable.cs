using UnityEngine;

namespace PointAndClickDemo.Interactables
{
    /// <summary>Something in the scene the player can interact with by clicking it.</summary>
    public interface IInteractable
    {
        bool CanBeInteracted(GameObject instigator);

        void Interact(GameObject instigator);
    }
}
