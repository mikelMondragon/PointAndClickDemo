using UnityEngine;

public enum CameraFramingMode
{
    /// <summary>El fondo entero cabe en pantalla. Aparecen barras si el aspect no coincide.</summary>
    Contain = 0,
    /// <summary>El fondo cubre la pantalla; se recorta lo que sobra y la cámara sigue al jugador.</summary>
    CoverFollow = 1,
}

/// <summary>
/// Matemática de encuadre, sin dependencias de escena para poder testearla aparte.
/// </summary>
public static class CameraFraming
{
    /// <summary>
    /// Tamaño ortográfico (media altura visible) necesario para encuadrar <paramref name="background"/>.
    /// Contain toma el mayor de los dos ajustes (cabe todo, sobra pantalla);
    /// Cover toma el menor (llena la pantalla, sobra fondo).
    /// </summary>
    public static float OrthographicSize(CameraFramingMode mode, Bounds background, float aspect)
    {
        if (aspect <= 0f) aspect = 1f;

        float fitToHeight = background.extents.y;
        float fitToWidth = background.extents.x / aspect;

        return mode == CameraFramingMode.Contain
            ? Mathf.Max(fitToHeight, fitToWidth)
            : Mathf.Min(fitToHeight, fitToWidth);
    }
}
