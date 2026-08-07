using UnityEngine;

public class CursorVisual : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    [SerializeField] Canvas canvas;
    [SerializeField] Camera cam;
    [SerializeField] LayerMask interactableLayer;
    [SerializeField] LayerMask walkableLayer;

    [Header("Sprites por estado")]
    [SerializeField] Sprite spriteDefault;
    [SerializeField] Sprite spriteWalkable;
    [SerializeField] Sprite spriteInteractable;

    UnityEngine.UI.Image image;

    void Awake() => image = GetComponent<UnityEngine.UI.Image>();

    void LateUpdate()
    {
        var pointer = PointerService.Instance.Current;

        Vector2 fromCenter = pointer.ScreenPosition - new Vector2(Screen.width, Screen.height) * 0.5f;
        rectTransform.anchoredPosition = fromCenter / canvas.scaleFactor;

        UpdateSprite(pointer.WorldPosition);
    }

    void UpdateSprite(Vector2 worldPoint)
    {
        if (Physics2D.OverlapPoint(worldPoint, interactableLayer) != null)
            image.sprite = spriteInteractable;
        else if (Physics2D.OverlapPoint(worldPoint, walkableLayer) != null)
            image.sprite = spriteWalkable;
        else
            image.sprite = spriteDefault;
    }
}