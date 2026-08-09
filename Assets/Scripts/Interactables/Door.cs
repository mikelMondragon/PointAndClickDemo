using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField]
    private float interactionDistance = 2f;

    public bool CanBeInteracted(GameObject instigator)
    {
        return Vector3.Distance(instigator.transform.position, this.transform.position) <= interactionDistance && instigator.GetComponent<Inventory>().CanUseDoor;
    }

    public void Interact(GameObject instigator)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}


