// GameCameraController.cs
// Provides dynamic camera control for the 3D Isometric / Perspective viewport.
// Allows live experimentation with:
// - Orthographic (2D Isometric) vs Perspective (3D depth & parallax)
// - Zoom / Orthographic Size (with the user's favorite P3 sweet spot = 3.4)
// - Field of View / Focal Length
// - Camera Distance / Dolly
// - Pitch & Yaw Orbit Angles
// - Mouse Wheel Zoom & Right-Click Orbit Drag in viewport
// - Floating in-game Camera Tuner HUD panel

using UnityEngine;

public class GameCameraController : MonoBehaviour
{
    public static GameCameraController Instance { get; private set; }

    [Header("Camera References")]
    public Camera targetCamera;
    public Transform backdropQuad;

    [Header("Projection & Tuning")]
    public bool isPerspective = false;
    public float orthoSize = 3.4f;         // P3 Sweet Spot default!
    public float perspectiveFov = 36f;     // Crisp focal length in 3D
    public float cameraDistance = 16.5f;   // Distance in 3D Perspective mode
    public float pitchAngle = 24f;
    public float yawAngle = -32f;
    public Vector3 boardCenter = new Vector3(8f, 0.35f, 2f);
    public Vector3 panOffset = Vector3.zero;

    [Header("State")]
    public bool userHasCustomized = false;
    public bool isTunerOpen = false;

    // Matrix Weave opts in; other levels retain their existing camera behavior.
    private bool fitLevelBounds;
    private Bounds levelBounds;
    public float BackdropDistance => fitLevelBounds
        ? Mathf.Max(32f, (isPerspective ? cameraDistance : 26f) + levelBounds.extents.magnitude + 12f)
        : 32f;
    private int framedPixelWidth;
    private int framedPixelHeight;

    public void SetLevelBounds(Bounds bounds, bool enabled)
    {
        levelBounds = bounds;
        fitLevelBounds = enabled;
        if (enabled) panOffset = Vector3.zero;
    }

    private Vector3 lastMousePos;
    private bool isOrbiting = false;
    private bool isPanning = false;

    private UIManager GetUI() => UIManager.Instance != null ? UIManager.Instance : Object.FindFirstObjectByType<UIManager>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void Start()
    {
        ApplyCameraSettings();
    }

    public void SetBoardCenter(Vector3 center, float levelDefaultOrtho = 3.4f)
    {
        boardCenter = center;
        if (!userHasCustomized)
        {
            // Default close like P3 (~3.4f), never pushed to far-away 6.4f!
            orthoSize = Mathf.Clamp(levelDefaultOrtho, 3.2f, 4.0f);
        }
        ApplyCameraSettings();
    }

    private void Update()
    {
        if (FrontScreenController.IsOpen) return;
        HandleKeyboardShortcuts();
        HandleViewportMouseInput();
        if (fitLevelBounds && targetCamera != null &&
            (targetCamera.pixelWidth != framedPixelWidth || targetCamera.pixelHeight != framedPixelHeight))
            ApplyCameraSettings();
    }

    private void HandleKeyboardShortcuts()
    {
        // 'P': Toggle Perspective <-> Orthographic
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleProjection();
        }

        // 'C': Toggle in-game Camera Tuner panel
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleTunerPanel();
        }

        // 'R': Reset to P3 Sweet Spot
        if (Input.GetKeyDown(KeyCode.R) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || !Input.anyKey))
        {
            // Only trigger bare R if not typing in an input field
            if (!Input.GetKey(KeyCode.LeftAlt))
                SetPresetP3SweetSpot();
        }

        // Hotkeys [ and ] for zoom
        if (Input.GetKey(KeyCode.LeftBracket))
        {
            AdjustZoom(-Time.deltaTime * 3f);
        }
        if (Input.GetKey(KeyCode.RightBracket))
        {
            AdjustZoom(Time.deltaTime * 3f);
        }
    }

    private void HandleViewportMouseInput()
    {
        // Only react to mouse when cursor is in the top 3D viewport (Y > 48% of screen)
        bool inViewport = Input.mousePosition.y > (Screen.height * 0.48f);

        // 1. Mouse Wheel Zoom
        float scroll = Input.mouseScrollDelta.y;
        if (inViewport && Mathf.Abs(scroll) > 0.01f)
        {
            AdjustZoom(-scroll * 0.5f);
        }

        // 2. Right-Click Drag to Orbit Pitch / Yaw
        if (inViewport && Input.GetMouseButtonDown(1))
        {
            isOrbiting = true;
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isOrbiting = false;
        }

        if (isOrbiting && Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            if (delta.sqrMagnitude > 0.01f)
            {
                userHasCustomized = true;
                yawAngle += delta.x * 0.28f;
                pitchAngle = Mathf.Clamp(pitchAngle - delta.y * 0.28f, 10f, 82f);
                ApplyCameraSettings();
                GetUI()?.UpdateCameraTunerReadout();
            }
        }

        // 3. Middle-Click Drag to Pan
        if (inViewport && Input.GetMouseButtonDown(2))
        {
            isPanning = true;
            lastMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(2))
        {
            isPanning = false;
        }

        if (isPanning && Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            lastMousePos = Input.mousePosition;

            if (delta.sqrMagnitude > 0.01f)
            {
                userHasCustomized = true;
                Quaternion rot = Quaternion.Euler(0f, yawAngle, 0f);
                Vector3 right = rot * Vector3.right;
                Vector3 forward = rot * Vector3.forward;

                float factor = (isPerspective ? cameraDistance * 0.0012f : orthoSize * 0.0022f);
                panOffset -= (right * delta.x + forward * delta.y) * factor;
                ApplyCameraSettings();
            }
        }
    }

    public void AdjustZoom(float delta)
    {
        userHasCustomized = true;
        if (isPerspective)
        {
            cameraDistance = Mathf.Clamp(cameraDistance + delta * 2.5f, 6f, 42f);
        }
        else
        {
            orthoSize = Mathf.Clamp(orthoSize + delta * 0.5f, 1.8f, 7.5f);
        }
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
    }

    public void AdjustFov(float delta)
    {
        userHasCustomized = true;
        perspectiveFov = Mathf.Clamp(perspectiveFov + delta, 16f, 75f);
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
    }

    public void AdjustPitch(float delta)
    {
        userHasCustomized = true;
        pitchAngle = Mathf.Clamp(pitchAngle + delta, 10f, 82f);
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
    }

    public void AdjustYaw(float delta)
    {
        userHasCustomized = true;
        yawAngle += delta;
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
    }

    public void ToggleProjection()
    {
        userHasCustomized = true;
        isPerspective = !isPerspective;
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
    }

    public void ToggleTunerPanel()
    {
        isTunerOpen = !isTunerOpen;
        GetUI()?.SetCameraTunerVisible(isTunerOpen);
    }

    // =========================================================================
    // One-Click Instant Presets
    // =========================================================================

    public void SetPresetP3SweetSpot()
    {
        userHasCustomized = true;
        isPerspective = false;
        orthoSize = 3.4f; // Exact P3 sweet spot!
        pitchAngle = 24f;
        yawAngle = -32f;
        panOffset = Vector3.zero;
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
        Debug.Log("GameCameraController: Loaded [P3 SWEET SPOT] Preset (Ortho 3.4, close & crisp).");
    }

    public void SetPreset3DPerspective()
    {
        userHasCustomized = true;
        isPerspective = true;
        perspectiveFov = 35f;
        cameraDistance = 16.5f;
        pitchAngle = 26f;
        yawAngle = -34f;
        panOffset = Vector3.zero;
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
        Debug.Log("GameCameraController: Loaded [3D PERSPECTIVE] Preset (FOV 35°, Distance 16.5m).");
    }

    public void SetPresetTacticalClose()
    {
        userHasCustomized = true;
        isPerspective = false;
        orthoSize = 2.85f; // Ultra close-up for inspection
        pitchAngle = 26f;
        yawAngle = -32f;
        panOffset = Vector3.zero;
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
        Debug.Log("GameCameraController: Loaded [TACTICAL CLOSE] Preset (Ortho 2.85).");
    }

    public void SetPresetCinematic()
    {
        userHasCustomized = true;
        isPerspective = true;
        perspectiveFov = 42f;
        cameraDistance = 18f;
        pitchAngle = 22f;
        yawAngle = -42f;
        panOffset = Vector3.zero;
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
        Debug.Log("GameCameraController: Loaded [CINEMATIC 3D] Preset (FOV 42°, Yaw -42°).");
    }

    public void SetPresetTopDown()
    {
        userHasCustomized = true;
        isPerspective = false;
        orthoSize = 3.6f;
        pitchAngle = 65f;
        yawAngle = -32f;
        panOffset = Vector3.zero;
        ApplyCameraSettings();
        GetUI()?.UpdateCameraTunerReadout();
        Debug.Log("GameCameraController: Loaded [TOP-DOWN] Preset (Pitch 65°).");
    }

    public void ApplyCameraSettings()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        targetCamera.rect = new Rect(0f, 0.48f, 1f, 0.52f);
        targetCamera.orthographic = !isPerspective;

        if (!isPerspective)
        {
            targetCamera.orthographicSize = orthoSize;
        }
        else
        {
            targetCamera.fieldOfView = perspectiveFov;
        }

        Quaternion rot = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        targetCamera.transform.rotation = rot;

        float dist = isPerspective ? cameraDistance : 26f;
        Vector3 target = boardCenter + panOffset;
        if (fitLevelBounds && !isPerspective)
        {
            // Fit the full platform envelope (including pillars and exit arch), not only tile centers.
            // Reserve the top22% for the title/controls,6% below and4.5% at the sides.
            const float topInset = 0.22f, bottomInset = 0.06f, sideInset = 0.045f;
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            Quaternion inverseRotation = Quaternion.Inverse(rot);
            Vector3 lo = levelBounds.min, hi = levelBounds.max;
            foreach (float x in new[] { lo.x, hi.x })
            foreach (float y in new[] { lo.y, hi.y })
            foreach (float z in new[] { lo.z, hi.z })
            {
                Vector3 projected = inverseRotation * (new Vector3(x, y, z) - target);
                minX = Mathf.Min(minX, projected.x); maxX = Mathf.Max(maxX, projected.x);
                minY = Mathf.Min(minY, projected.y); maxY = Mathf.Max(maxY, projected.y);
            }
            float usableHeight = 1f - topInset - bottomInset;
            float usableWidth = 1f - sideInset * 2f;
            float needed = Mathf.Max((maxY - minY) / (2f * usableHeight),
                (maxX - minX) / (2f * Mathf.Max(0.1f, targetCamera.aspect) * usableWidth));
            orthoSize = Mathf.Max(orthoSize, needed * 1.04f);
            targetCamera.orthographicSize = orthoSize;
            // Place the board below the header while centering its projected geometry horizontally.
            target += rot * new Vector3((minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f + orthoSize * (topInset - bottomInset), 0f);
        }
        targetCamera.transform.position = target - (rot * Vector3.forward) * dist;
        framedPixelWidth = targetCamera.pixelWidth;
        framedPixelHeight = targetCamera.pixelHeight;

        UpdateBackdropQuad();
    }

    private void UpdateBackdropQuad()
    {
        if (backdropQuad == null && targetCamera != null)
        {
            backdropQuad = targetCamera.transform.Find("FactoryBackdropQuad");
        }
        if (backdropQuad == null) return;

        float h = (!isPerspective)
            ? targetCamera.orthographicSize * 2.2f
            : 2f * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * BackdropDistance;
        float w = h * Mathf.Max(3.6f, targetCamera.aspect * 1.05f);

        backdropQuad.localPosition = new Vector3(0f, 0f, BackdropDistance);
        backdropQuad.localRotation = Quaternion.identity;
        backdropQuad.localScale = new Vector3(w, h, 1f);
    }
}
