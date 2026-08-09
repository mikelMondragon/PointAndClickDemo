using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Encuadra la cámara sobre el fondo con dos estrategias intercambiables en runtime:
///
///   Contain      la cámara ajusta para que quepa el fondo entero; no se mueve.
///   CoverFollow  la cámara ajusta para cubrir la pantalla y un rig de Cinemachine
///                sigue al jugador, confinado a los límites del fondo.
///
/// El rig de Cinemachine se construye por código en Play para no ensuciar la escena
/// ni depender de wiring manual.
/// </summary>
[ExecuteAlways, RequireComponent(typeof(Camera))]
public class CameraFramingController : MonoBehaviour
{
    [SerializeField] SpriteRenderer background;
    [SerializeField] Color barColor = Color.black;

    [Header("Encuadre")]
    [SerializeField] CameraFramingMode mode = CameraFramingMode.Contain;
    [SerializeField, Tooltip("Si se deja vacío se busca el PlayerController de la escena")]
    Transform followTarget;
    [SerializeField, Range(0.001f, 0.05f),
     Tooltip("Zoom extra en Cover. Evita que la vista encaje al milímetro con el fondo, " +
             "que es lo que degenera el polígono del confinador y clava la cámara")]
    float coverOverscan = 0.005f;

    [Header("Seguimiento (solo en Cover)")]
    [SerializeField, Tooltip("Suavizado del seguimiento en X/Y, en segundos")]
    Vector2 followDamping = new(0.5f, 0.5f);
    [SerializeField, Range(0f, 1f), Tooltip("Fracción de pantalla en la que el jugador se mueve sin arrastrar la cámara")]
    float horizontalDeadZone = 0.25f;

    [Header("HUD")]
    [SerializeField, Tooltip("Construye en runtime el desplegable para cambiar de modo")]
    bool buildHud = true;

    Camera cam;
    CinemachineBrain brain;
    CinemachineCamera coverCamera;
    CinemachineConfiner2D confiner;
    BoxCollider2D confinerShape;
    Vector2Int lastScreenSize;

    /// <summary>Modo activo. Cambiarlo reconfigura la cámara al instante.</summary>
    public CameraFramingMode Mode
    {
        get => mode;
        set
        {
            if (mode == value) return;
            mode = value;
            ApplyMode();
        }
    }

    public event System.Action<CameraFramingMode> ModeChanged;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        if (Application.isPlaying)
        {
            ResolveFollowTarget();
            BuildCinemachineRig();
            if (buildHud) CameraModeHud.Create(this);
        }

        ApplyMode();
    }

    void LateUpdate()
    {
        if (background == null || cam == null) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = barColor;

        // El aspect solo cambia al redimensionar la ventana: no hace falta recalcular cada frame.
        Vector2Int screen = new(Screen.width, Screen.height);
        if (screen != lastScreenSize)
        {
            lastScreenSize = screen;
            RefreshFraming();
        }

        if (!IsCoverActive) CenterOnBackground();
    }

    bool IsCoverActive => Application.isPlaying
        && mode == CameraFramingMode.CoverFollow
        && coverCamera != null;

    void ApplyMode()
    {
        RefreshFraming();

        bool cover = IsCoverActive;

        if (cover)
        {
            // El jugador puede haberse instanciado después del arranque.
            if (followTarget == null) ResolveFollowTarget();
            coverCamera.Follow = followTarget;
            if (followTarget == null)
                Debug.LogWarning("Modo Cover sin follow target: la cámara no seguirá a nadie.", this);
        }

        if (brain != null) brain.enabled = cover;
        if (coverCamera != null) coverCamera.gameObject.SetActive(cover);
        if (!cover) CenterOnBackground();

        ModeChanged?.Invoke(mode);
    }

    void RefreshFraming()
    {
        if (background == null || cam == null) return;

        Bounds bounds = background.bounds;
        float size = CameraFraming.OrthographicSize(mode, bounds, cam.aspect);

        // En Cover la vista encajaría exactamente con el fondo en uno de los ejes.
        // El confinador resta el semi-tamaño de la cámara a la forma, así que ese eje
        // le quedaría con longitud cero y la cámara acabaría fijada en un punto.
        // Un zoom mínimo extra le deja holgura real por ambos lados.
        if (mode == CameraFramingMode.CoverFollow) size *= 1f - coverOverscan;

        cam.orthographicSize = size;

        if (coverCamera != null)
        {
            coverCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            coverCamera.Lens.OrthographicSize = size;
        }

        if (confinerShape != null)
        {
            confinerShape.transform.position = new Vector3(bounds.center.x, bounds.center.y, 0f);
            confinerShape.size = bounds.size;
        }

        // El confinador cachea la forma en función del tamaño de la lente.
        if (confiner != null) confiner.InvalidateLensCache();
    }

    void CenterOnBackground()
    {
        Bounds bounds = background.bounds;
        transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
    }

    void ResolveFollowTarget()
    {
        if (followTarget != null) return;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) followTarget = player.transform;
    }

    void BuildCinemachineRig()
    {
        if (coverCamera != null || background == null) return;

        brain = GetComponent<CinemachineBrain>();
        if (brain == null) brain = gameObject.AddComponent<CinemachineBrain>();

        // Se monta desactivado para que ningún OnEnable corra con el rig a medias:
        // la CinemachineCamera cachea su pipeline a partir de los componentes presentes.
        GameObject rig = new("CoverFollowCamera");
        rig.SetActive(false);
        rig.transform.SetParent(transform.parent, false);

        coverCamera = rig.AddComponent<CinemachineCamera>();
        coverCamera.Follow = followTarget;
        coverCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;

        CinemachinePositionComposer composer = rig.AddComponent<CinemachinePositionComposer>();
        composer.CameraDistance = Mathf.Max(1f, Mathf.Abs(transform.position.z));
        composer.Damping = new Vector3(followDamping.x, followDamping.y, 0f);
        composer.Composition.DeadZone.Enabled = true;
        // Zona muerta vertical al máximo: en Cover el eje Y no aporta nada y así
        // el composer no pelea con el confinador por la poca holgura que hay.
        composer.Composition.DeadZone.Size = new Vector2(horizontalDeadZone, 2f);

        // La forma del confinador vive en la raíz: si colgase del rig se movería con él.
        GameObject shape = new("CameraBounds") { layer = 0 };
        confinerShape = shape.AddComponent<BoxCollider2D>();
        confinerShape.isTrigger = true;

        confiner = rig.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = confinerShape;
    }

    // Fuerza un reencuadre si se toca el fondo o el modo desde el inspector.
    void OnValidate() => lastScreenSize = Vector2Int.zero;

    void OnDestroy()
    {
        if (coverCamera != null) Destroy(coverCamera.gameObject);
        if (confinerShape != null) Destroy(confinerShape.gameObject);
    }
}
