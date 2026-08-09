using UnityEngine;

public class Collectable : MonoBehaviour, IInteractable
{
    [SerializeField]
    private float interactionDistance = 2f;
    public bool CanBeInteracted(GameObject instigator)
    {
        return Vector3.Distance(instigator.transform.position, this.transform.position) <= interactionDistance;
    }

    public void Interact(GameObject instigator)
    {
        instigator.GetComponent<Inventory>().CanUseDoor = true;
        Destroy(this.gameObject);
    }
}
