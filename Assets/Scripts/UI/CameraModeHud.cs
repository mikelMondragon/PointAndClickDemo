using System.Collections.Generic;
using PointAndClickDemo.Cameras;
using UnityEngine;
using UnityEngine.UI;

namespace PointAndClickDemo.UI
{
    /// <summary>
    /// Dropdown to switch the camera framing at runtime.
    /// It is built from code on its own responsive Canvas, so it needs no scene
    /// wiring and does not interfere with the cursor's Canvas.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CameraModeHud : MonoBehaviour
    {
        private const int FontSize = 20;
        private const float ItemHeight = 40f;

        private static readonly Color PanelColor = new(0.09f, 0.09f, 0.11f, 0.92f);
        private static readonly Color AccentColor = new(0.35f, 0.68f, 1f, 1f);
        private static readonly Color TextColor = new(0.93f, 0.93f, 0.95f, 1f);

        private CameraFramingController controller;
        private Dropdown dropdown;

        public static CameraModeHud Create(CameraFramingController controller)
        {
            GameObject root = new(
                "CameraModeHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            CameraModeHud hud = root.AddComponent<CameraModeHud>();
            hud.controller = controller;
            hud.Build();
            return hud;
        }

        private void OnDestroy()
        {
            if (controller != null) controller.ModeChanged -= OnModeChanged;
        }

        private void Build()
        {
            SetUpCanvas();

            GameObject dropdownObject = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            dropdownObject.name = "CameraModeDropdown";
            dropdownObject.transform.SetParent(transform, false);

            RectTransform rect = (RectTransform)dropdownObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);
            rect.sizeDelta = new Vector2(420f, 52f);

            dropdown = dropdownObject.GetComponent<Dropdown>();

            // The template "Item" is 20px tall and its label only gets 17 usable: at font
            // size 20 the text does not fit, and Text (Truncate) then draws nothing at all.
            if (dropdown.itemText != null)
            {
                RectTransform item = (RectTransform)dropdown.itemText.transform.parent;
                item.sizeDelta = new Vector2(item.sizeDelta.x, ItemHeight);
            }

            Style(dropdownObject);

            dropdown.options = new List<Dropdown.OptionData>
            {
                new("Contain - full scene"),
                new("Cover - camera follows player"),
            };
            dropdown.SetValueWithoutNotify((int)controller.Mode);
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(OnDropdownChanged);

            controller.ModeChanged += OnModeChanged;
        }

        private void SetUpCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1; // below the cursor's canvas

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void OnDropdownChanged(int index) => controller.Mode = (CameraFramingMode)index;

        private void OnModeChanged(CameraFramingMode newMode)
        {
            if (dropdown == null) return;

            dropdown.SetValueWithoutNotify((int)newMode);
            dropdown.RefreshShownValue();
        }

        /// <summary>
        /// DefaultControls builds the dropdown with empty sprites; it is tinted by hand
        /// so it stays readable without depending on any UI asset.
        /// </summary>
        private static void Style(GameObject root)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                string objectName = image.gameObject.name;

                if (objectName.Contains("Arrow") || objectName.Contains("Checkmark"))
                    image.color = AccentColor;
                else if (objectName.Contains("Item Background"))
                    image.color = new Color(1f, 1f, 1f, 0.06f);
                else
                    image.color = PanelColor;
            }

            Font fallbackFont = ResolveFont();

            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text.font == null) text.font = fallbackFont;

                text.color = TextColor;
                text.fontSize = FontSize;
                text.alignment = TextAnchor.MiddleLeft;

                // Without this, a line that does not fit its rect is not drawn at all.
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        /// <summary>Safety net in case the built-in font does not resolve at runtime.</summary>
        private static Font ResolveFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) return font;

            string[] installed = Font.GetOSInstalledFontNames();
            return installed is { Length: > 0 }
                ? Font.CreateDynamicFontFromOSFont(installed[0], FontSize)
                : null;
        }
    }
}
