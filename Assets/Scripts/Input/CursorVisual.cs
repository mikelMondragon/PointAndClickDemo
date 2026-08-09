using UnityEngine;
using UnityEngine.UI;

namespace PointAndClickDemo.Input
{
    /// <summary>
    /// Draws the custom cursor and swaps its sprite based on what sits under the pointer.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CursorVisual : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera cam;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private LayerMask walkableLayer;

        [Header("Sprites per state")]
        [SerializeField] private Sprite spriteDefault;
        [SerializeField] private Sprite spriteWalkable;
        [SerializeField] private Sprite spriteInteractable;

        private Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
            if (rectTransform == null) rectTransform = (RectTransform)transform;
        }

        private void LateUpdate()
        {
            if (PointerService.Instance == null) return;

            IPointerSource pointer = PointerService.Instance.Current;

            Vector2 fromCenter = pointer.ScreenPosition - new Vector2(Screen.width, Screen.height) * 0.5f;
            rectTransform.anchoredPosition = fromCenter / canvas.scaleFactor;

            UpdateSprite(pointer.WorldPosition);
        }

        private void UpdateSprite(Vector2 worldPoint)
        {
            if (Physics2D.OverlapPoint(worldPoint, interactableLayer) != null)
                image.sprite = spriteInteractable;
            else if (Physics2D.OverlapPoint(worldPoint, walkableLayer) != null)
                image.sprite = spriteWalkable;
            else
                image.sprite = spriteDefault;
        }
    }
}
