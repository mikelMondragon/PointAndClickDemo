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
        {
            Debug.LogError($"{pathProviderSource} no implementa IPathProvider", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (PointerService.Instance == null) return;
        if (!PointerService.Instance.Current.IsPressed) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        ResolveClick(PointerService.Instance.Current.WorldPosition);
    }

    void ResolveClick(Vector2 world)
    {
        // Si el click resuelve una interacción, el personaje no se mueve.
        // Si el objeto existe pero está fuera de alcance, caminamos hacia él.
        if (TryInteract(world)) return;
        MoveTo(world);
    }

    bool TryInteract(Vector2 world)
    {
        Collider2D hit = Physics2D.OverlapPoint(world, interactableLayer);
        if (hit == null) return false;

        IInteractable interactable = hit.GetComponent<IInteractable>();
        if (interactable == null || !interactable.CanBeInteracted(gameObject)) return false;

        interactable.Interact(gameObject);
        return true;
    }

    void MoveTo(Vector2 world)
    {
        // Destino inalcanzable: se ignora el click y se conserva la ruta actual.
        if (!pathProvider.TryGetPath(transform.position, world, pathBuffer)) return;

        movement.SetPath(pathBuffer);
        DrawDebugPath();
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
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
