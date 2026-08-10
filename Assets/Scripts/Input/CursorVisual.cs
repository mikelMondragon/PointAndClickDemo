using UnityEngine;
using UnityEngine.UI;

namespace PointAndClickDemo.Input
{
    /// <summary>
    /// Draws the custom cursor and gives feedback about what sits under the pointer:
    /// blue over an interactable, green over walkable ground, red over anything else.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CursorVisual : MonoBehaviour
    {
        private enum CursorState
        {
            Blocked,
            Walkable,
            Interactable,
        }

        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera cam;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private LayerMask walkableLayer;

        [Header("Sprites per state")]
        [SerializeField]
        [Tooltip("Nothing under the pointer")]
        private Sprite spriteDefault;

        [SerializeField] private Sprite spriteWalkable;
        [SerializeField] private Sprite spriteInteractable;

        [Header("Colours per state")]
        [SerializeField]
        [Tooltip("Nothing under the pointer")]
        private Color colorDefault = new(0.90f, 0.28f, 0.28f, 1f);

        [SerializeField] private Color colorWalkable = new(0.36f, 0.84f, 0.42f, 1f);
        [SerializeField] private Color colorInteractable = new(0.35f, 0.68f, 1.00f, 1f);

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

            UpdateVisual(pointer.WorldPosition);
        }

        private void UpdateVisual(Vector2 worldPoint)
        {
            // The sprite and the colour are picked together so the two can never
            // describe different states.
            (Sprite sprite, Color color) = ResolveState(worldPoint) switch
            {
                CursorState.Interactable => (spriteInteractable, colorInteractable),
                CursorState.Walkable => (spriteWalkable, colorWalkable),
                _ => (spriteDefault, colorDefault),
            };

            image.sprite = sprite;
            image.color = color;
        }

        /// <summary>
        /// Interactables win over walkable ground: they sit on top of it, and being able
        /// to interact is more relevant to the player than being able to walk there.
        /// </summary>
        private CursorState ResolveState(Vector2 worldPoint)
        {
            if (Physics2D.OverlapPoint(worldPoint, interactableLayer) != null) return CursorState.Interactable;
            if (Physics2D.OverlapPoint(worldPoint, walkableLayer) != null) return CursorState.Walkable;

            return CursorState.Blocked;
        }
    }
}
