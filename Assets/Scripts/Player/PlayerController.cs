using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    void Update()
    {
        if (!PointerService.Instance.Current.IsPressed) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Vector2 world = PointerService.Instance.Current.WorldPosition;
        ResolveClick(world);
    }

    void ResolveClick(Vector2 world)
    {
        RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero);
        if (hit.collider != null)
        {
            Debug.Log($"Clicked on: {hit.collider.gameObject.name}");
        }
    }
}