using UnityEngine;

public interface IInteractable
{
    void Interact(GameObject instigator);
    bool CanBeInteracted(GameObject instigator);
}
