using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Desplegable para cambiar el encuadre de cámara en runtime.
/// Se construye por código sobre su propio Canvas responsive, así que no requiere
/// wiring en la escena ni interfiere con el Canvas del cursor.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CameraModeHud : MonoBehaviour
{
    const int FontSize = 20;
    const float ItemHeight = 40f;

    static readonly Color Panel = new(0.09f, 0.09f, 0.11f, 0.92f);
    static readonly Color Accent = new(0.35f, 0.68f, 1f, 1f);
    static readonly Color LabelColor = new(0.93f, 0.93f, 0.95f, 1f);

    CameraFramingController controller;
    Dropdown dropdown;

    public static CameraModeHud Create(CameraFramingController controller)
    {
        GameObject root = new("CameraModeHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        CameraModeHud hud = root.AddComponent<CameraModeHud>();
        hud.controller = controller;
        hud.Build();
        return hud;
    }

    void Build()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1; // por debajo del canvas del cursor

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject go = DefaultControls.CreateDropdown(new DefaultControls.Resources());
        go.name = "CameraModeDropdown";
        go.transform.SetParent(transform, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(420f, 52f);

        dropdown = go.GetComponent<Dropdown>();

        // El "Item" plantilla mide 20px de alto y su label deja 17 útiles: con la
        // fuente a 20 el texto no cabe y Text (Truncate) no dibuja nada. Se agranda.
        if (dropdown.itemText != null)
        {
            RectTransform item = (RectTransform)dropdown.itemText.transform.parent;
            item.sizeDelta = new Vector2(item.sizeDelta.x, ItemHeight);
        }

        Style(go);

        dropdown.options = new List<Dropdown.OptionData>
        {
            new("Contain - escena completa"),
            new("Cover - camara sigue al jugador"),
        };
        dropdown.SetValueWithoutNotify((int)controller.Mode);
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(OnDropdownChanged);

        controller.ModeChanged += OnModeChanged;
    }

    void OnDropdownChanged(int index) => controller.Mode = (CameraFramingMode)index;

    void OnModeChanged(CameraFramingMode newMode)
    {
        if (dropdown == null) return;
        dropdown.SetValueWithoutNotify((int)newMode);
        dropdown.RefreshShownValue();
    }

    void OnDestroy()
    {
        if (controller != null) controller.ModeChanged -= OnModeChanged;
    }

    /// <summary>
    /// DefaultControls crea el desplegable con sprites vacíos; se tiñe a mano para
    /// que sea legible sin depender de assets de UI.
    /// </summary>
    static void Style(GameObject root)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            string n = image.gameObject.name;
            if (n.Contains("Arrow") || n.Contains("Checkmark")) image.color = Accent;
            else if (n.Contains("Item Background")) image.color = new Color(1f, 1f, 1f, 0.06f);
            else image.color = Panel;
        }

        Font fallback = ResolveFont();

        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            if (text.font == null) text.font = fallback;
            text.color = LabelColor;
            text.fontSize = FontSize;
            text.alignment = TextAnchor.MiddleLeft;
            // Sin esto, una línea que no quepa en su rect no se dibuja en absoluto.
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    /// <summary>Red de seguridad por si la fuente built-in no resuelve en runtime.</summary>
    static Font ResolveFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null) return font;

        string[] installed = Font.GetOSInstalledFontNames();
        return installed is { Length: > 0 } ? Font.CreateDynamicFontFromOSFont(installed[0], FontSize) : null;
    }
}
