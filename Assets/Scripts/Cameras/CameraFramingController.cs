using System;
using PointAndClickDemo.Characters.Player;
using PointAndClickDemo.UI;
using Unity.Cinemachine;
using UnityEngine;

namespace PointAndClickDemo.Cameras
{
    /// <summary>
    /// Frames the camera over the background using two strategies that can be swapped at runtime:
    ///
    ///   Contain      the camera fits the whole background on screen and never moves.
    ///   CoverFollow  the camera fills the screen and a Cinemachine rig follows the player,
    ///                confined to the background bounds.
    ///
    /// The Cinemachine rig is built from code on Play so the scene stays clean and no
    /// manual wiring is required.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CameraFramingController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Color barColor = Color.black;

        [Header("Framing")]
        [SerializeField] private CameraFramingMode mode = CameraFramingMode.Contain;

        [SerializeField]
        [Tooltip("Left empty, the PlayerController in the scene is used")]
        private Transform followTarget;

        [SerializeField]
        [Range(0.001f, 0.05f)]
        [Tooltip("Extra zoom in Cover mode. Stops the view from matching the background exactly, " +
                 "which is what degenerates the confiner polygon and pins the camera in place")]
        private float coverOverscan = 0.005f;

        [Header("Follow (Cover mode only)")]
        [SerializeField]
        [Tooltip("Follow damping on X/Y, in seconds")]
        private Vector2 followDamping = new(0.5f, 0.5f);

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the screen the player can move across without dragging the camera")]
        private float horizontalDeadZone = 0.25f;

        [Header("HUD")]
        [SerializeField]
        [Tooltip("Builds the mode dropdown at runtime")]
        private bool buildHud = true;

        private Camera cam;
        private CinemachineBrain brain;
        private CinemachineCamera coverCamera;
        private CinemachineConfiner2D confiner;
        private BoxCollider2D confinerShape;
        private Vector2Int lastScreenSize;

        /// <summary>Raised every time a mode is applied, including the initial one.</summary>
        public event Action<CameraFramingMode> ModeChanged;

        /// <summary>Active mode. Setting it reconfigures the camera immediately.</summary>
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

        private bool IsCoverActive => Application.isPlaying
                                      && mode == CameraFramingMode.CoverFollow
                                      && coverCamera != null;

        private void OnEnable()
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

        private void LateUpdate()
        {
            if (background == null || cam == null) return;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = barColor;

            // The aspect only changes when the window is resized, so there is no need
            // to recompute the framing every frame.
            Vector2Int screenSize = new(Screen.width, Screen.height);
            if (screenSize != lastScreenSize)
            {
                lastScreenSize = screenSize;
                RefreshFraming();
            }

            if (!IsCoverActive) CenterOnBackground();
        }

        // Forces a reframe when the background or the mode is edited in the inspector.
        private void OnValidate() => lastScreenSize = Vector2Int.zero;

        private void OnDestroy()
        {
            if (coverCamera != null) Destroy(coverCamera.gameObject);
            if (confinerShape != null) Destroy(confinerShape.gameObject);
        }

        private void ApplyMode()
        {
            RefreshFraming();

            bool cover = IsCoverActive;

            if (cover)
            {
                // The player may have been spawned after start-up.
                if (followTarget == null) ResolveFollowTarget();

                coverCamera.Follow = followTarget;
                if (followTarget == null)
                    Debug.LogWarning("Cover mode has no follow target: the camera will not follow anyone.", this);
            }

            if (brain != null) brain.enabled = cover;
            if (coverCamera != null) coverCamera.gameObject.SetActive(cover);
            if (!cover) CenterOnBackground();

            ModeChanged?.Invoke(mode);
        }

        private void RefreshFraming()
        {
            if (background == null || cam == null) return;

            Bounds bounds = background.bounds;
            float size = CameraFraming.OrthographicSize(mode, bounds, cam.aspect);

            // In Cover mode the view would match the background exactly on one axis.
            // The confiner shrinks the shape by the camera half-extents, so that axis
            // would end up with zero length and the camera would be pinned to a point.
            // A minimal extra zoom leaves real slack on both sides.
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

            // The confiner caches the shape against the current lens size.
            if (confiner != null) confiner.InvalidateLensCache();
        }

        private void CenterOnBackground()
        {
            Bounds bounds = background.bounds;
            transform.position = new Vector3(bounds.center.x, bounds.center.y, transform.position.z);
        }

        private void ResolveFollowTarget()
        {
            if (followTarget != null) return;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) followTarget = player.transform;
        }

        private void BuildCinemachineRig()
        {
            if (coverCamera != null || background == null) return;

            brain = GetComponent<CinemachineBrain>();
            if (brain == null) brain = gameObject.AddComponent<CinemachineBrain>();

            // Built inactive so no OnEnable runs against a half-assembled rig:
            // CinemachineCamera caches its pipeline from the components present at that point.
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

            // A maxed-out vertical dead zone: the Y axis adds nothing in Cover mode, and this
            // keeps the composer from fighting the confiner over the little slack there is.
            composer.Composition.DeadZone.Size = new Vector2(horizontalDeadZone, 2f);

            // The confiner shape lives at the root: parented to the rig it would move with it.
            GameObject shape = new("CameraBounds");
            confinerShape = shape.AddComponent<BoxCollider2D>();
            confinerShape.isTrigger = true;

            confiner = rig.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = confinerShape;
        }
    }
}
