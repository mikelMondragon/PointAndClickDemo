using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    [SerializeField] MonoBehaviour pathProviderSource;
    [SerializeField] LayerMask interactableLayer;
    [SerializeField] CharacterMovement movement;

    IPathProvider pathProvider;
    readonly List<Vector2> pathBuffer = new();

    void Awake()
    {
        pathProvider = pathProviderSource as IPathProvider;
        if (pathProvider == null)
            Debug.LogError($"{pathProviderSource} no implementa IPathProvider", this);
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
        Collider2D interactable = Physics2D.OverlapPoint(world, interactableLayer);
        if (interactable != null)
        {
            Debug.Log($"Clicked on: {interactable.gameObject.name}");
            return;
        }

        if (pathProvider.TryGetPath(transform.position, world, pathBuffer))
        {
            movement.SetPath(pathBuffer);
            DrawDebugPath();
        }
        else
        {
            Debug.Log("Destino inalcanzable.");
        }
    }

    void DrawDebugPath()
    {
        Vector2 previous = transform.position;
        foreach (Vector2 point in pathBuffer)
        {
            Debug.DrawLine(previous, point, Color.yellow, 2f);
            previous = point;
        }
    }
}