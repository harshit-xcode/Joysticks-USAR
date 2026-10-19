// ClaudeMascotAnimator.cs
// Builds and animates Claude's iconic blocky terracotta mascot ("Clawd") in 3D:
// - Wide rectangular bevel-edged orange body with the signature top-left vertical notch
// - Two stubby side arms and 8 blocky legs (4 front + 4 back in left/right pairs)
// - Expressive 3D animated eyes:
//     * Normal/Walk: Vertical block eyes '|  |' with natural blinking & directional gaze
//     * Jump/Effort/Bump/Hurt: Iconic 3D chevron eyes '>  <'
//     * Victory/Win: Happy arc eyes '^  ^' with rosy cheek glow
//     * Knocked Out/Dead: Crossed 'X  X' eyes with cartoon singed hop
// - Procedural animations for Idle, Walk (8-leg crab/octo scuttle), Jump (squash, stretch, tuck),
//   Pulse (EMP spin), Bump (head shake), Death, and Victory dance.

using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ClaudeMascotAnimator : MonoBehaviour
{
    public enum EyeExpression
    {
        NormalVertical, // |   |  (Image 1 default, with blinking & gaze shift)
        ChevronSqueeze, // >   <  (Image 2 jump / effort / bump / hurt)
        HappyArcs,      // ^   ^  (Victory celebration)
        DizzyCross      // X   X  (Lava / spike knockout)
    }

    [Header("References")]
    public PlayerController player;
    public Transform mascotPivot;
    public Transform bodyGroup;
    public Transform leftArmPivot;
    public Transform rightArmPivot;
    public List<Transform> legPivots = new List<Transform>();

    [Header("Facial Expression Roots")]
    public Transform verticalEyesRoot;
    public Transform leftVerticalEye;
    public Transform rightVerticalEye;
    public Transform chevronEyesRoot;
    public Transform happyEyesRoot;
    public Transform dizzyEyesRoot;
    public Transform blushRoot;

    // Animation state tracking
    private EyeExpression currentExpression = EyeExpression.NormalVertical;
    private float walkTimer = 0f;
    private float walkDuration = 0f;
    private Vector2Int walkDirection = new Vector2Int(1, 0);

    private float jumpTimer = 0f;
    private float jumpDuration = 0f;

    private float bumpTimer = 0f;
    private float bumpDuration = 0f;
    private Vector2Int bumpDirection = new Vector2Int(1, 0);

    private float pulseTimer = 0f;
    private float pulseDuration = 0f;

    private float landSquashTimer = 0f;

    // Blink & idle gaze state
    private float nextBlinkTime = 2.0f;
    private float blinkTimer = -1f;
    private const float BlinkDuration = 0.13f;
    private Vector2 targetGazeOffset = Vector2.zero;
    private Vector2 currentGazeOffset = Vector2.zero;
    private float nextGazeChangeTime = 1.5f;

    // Presentation yaw is computed in WORLD space from the camera so Clawd's face is always visible
    // (parent PlayerController rotation differs between Edit Mode and Play Mode).
    // A small turn toward +X (the track direction) gives the 3/4 look from the reference image.
    private const float BasePresentationYaw = -14f; // offset relative to "facing camera"
    private const float MascotOverallScale = 1.32f;
    private float currentPresentationYaw = BasePresentationYaw;
    private float spinOffset = 0f;

    private static float CameraFacingYaw()
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.eulerAngles.y + 180f : 148f;
    }

    private void ApplyPivotRotation(float relativeYaw)
    {
        mascotPivot.rotation = Quaternion.Euler(0f, CameraFacingYaw() + relativeYaw, 0f);
    }

    private Vector3 baseBodyLocalPos = new Vector3(0f, 0.56f, 0f);

    public static ClaudeMascotAnimator BuildOn(PlayerController player, Transform visualRoot)
    {
        var anim = visualRoot.gameObject.GetComponent<ClaudeMascotAnimator>();
        if (anim == null) anim = visualRoot.gameObject.AddComponent<ClaudeMascotAnimator>();
        anim.player = player;
        anim.ConstructGeometry(visualRoot);
        return anim;
    }

    public void ConstructGeometry(Transform visualRoot)
    {
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(visualRoot.GetChild(i).gameObject);
        }
        legPivots.Clear();

        // Rich multi-tone terracotta palette so the 3D bevels, arms, legs, and notch pop in isometric lighting
        Material frontFaceOrange = IsometricBoardBuilder.GetOrCreateMaterial(
            "ClaudeFrontFaceOrange", new Color(0.95f, 0.46f, 0.22f), false, 0.42f);
        Material mainOrange = IsometricBoardBuilder.GetOrCreateMaterial(
            "ClaudeMainOrange", new Color(0.89f, 0.40f, 0.18f), false, 0.38f);
        Material topBevelOrange = IsometricBoardBuilder.GetOrCreateMaterial(
            "ClaudeTopBevelOrange", new Color(0.99f, 0.56f, 0.31f), false, 0.48f);
        Material sideShadeOrange = IsometricBoardBuilder.GetOrCreateMaterial(
            "ClaudeSideShadeOrange", new Color(0.74f, 0.30f, 0.12f), false, 0.32f);
        Material backLegOrange = IsometricBoardBuilder.GetOrCreateMaterial(
            "ClaudeBackLegOrange", new Color(0.60f, 0.23f, 0.09f), false, 0.28f);
        Material creviceDark = IsometricBoardBuilder.GetOrCreateMaterial(
            "ClaudeCreviceDark", new Color(0.18f, 0.06f, 0.03f), false, 0.15f);
        Material eyeBlack = IsometricBoardBuilder.GetOrCreateMaterial(
            "ClaudeEyeBlack", new Color(0.03f, 0.03f, 0.04f), false, 0.65f);
        Material blushPink = IsometricBoardBuilder.GetOrCreateMaterial(
            "ClaudeBlushPink", new Color(1.00f, 0.30f, 0.26f), true, 0.50f);

        // Master pivot for 3/4 isometric presentation angle & overall scale
        GameObject pivotObj = new GameObject("MascotPivot");
        pivotObj.transform.SetParent(visualRoot, false);
        pivotObj.transform.localPosition = new Vector3(0f, -0.08f, 0f);
        pivotObj.transform.localRotation = Quaternion.Euler(0f, BasePresentationYaw, 0f);
        pivotObj.transform.localScale = Vector3.one * MascotOverallScale;
        mascotPivot = pivotObj.transform;
        ApplyPivotRotation(BasePresentationYaw);

        // ---------------------------------------------------------------------
        // 1. BODY GROUP (Wide rectangular chamfered block + top-left notch)
        // ---------------------------------------------------------------------
        GameObject bodyObj = new GameObject("BodyGroup");
        bodyObj.transform.SetParent(mascotPivot, false);
        bodyObj.transform.localPosition = baseBodyLocalPos;
        bodyGroup = bodyObj.transform;

        // Core torso block (wide X = 0.96, height Y = 0.62, depth Z = 0.52)
        MakeBox(bodyGroup, "BodyCore", Vector3.zero, new Vector3(0.96f, 0.62f, 0.52f), mainOrange);

        // Raised chamfered front & back face panels (gives the crisp beveled frame from Image 1)
        MakeBox(bodyGroup, "FrontFacePanel", new Vector3(0f, 0f, 0.258f), new Vector3(0.88f, 0.54f, 0.030f), frontFaceOrange);
        MakeBox(bodyGroup, "BackFacePanel", new Vector3(0f, 0f, -0.258f), new Vector3(0.88f, 0.54f, 0.030f), sideShadeOrange);

        // Side & bottom bevel panels with distinct shading for 3D crispness
        MakeBox(bodyGroup, "LeftSideBevel", new Vector3(-0.478f, 0f, 0f), new Vector3(0.026f, 0.54f, 0.44f), sideShadeOrange);
        MakeBox(bodyGroup, "RightSideBevel", new Vector3(0.478f, 0f, 0f), new Vector3(0.026f, 0.54f, 0.44f), topBevelOrange);
        MakeBox(bodyGroup, "UnderbellyPanel", new Vector3(0f, -0.308f, 0f), new Vector3(0.88f, 0.028f, 0.44f), backLegOrange);

        // Top bevel split by the signature Top-Left Notch above the left eye (Image 1 detail!)
        MakeBox(bodyGroup, "TopBevelRight", new Vector3(-0.115f, 0.308f, 0f), new Vector3(0.65f, 0.030f, 0.45f), topBevelOrange);
        MakeBox(bodyGroup, "TopBevelLeft", new Vector3(0.365f, 0.308f, 0f), new Vector3(0.15f, 0.030f, 0.45f), topBevelOrange);

        // Recessed Top-Left Notch / Slot (carved down from the top edge toward the left eye)
        MakeBox(bodyGroup, "SignatureTopLeftNotchFront", new Vector3(0.245f, 0.21f, 0.165f), new Vector3(0.055f, 0.22f, 0.24f), creviceDark);
        MakeBox(bodyGroup, "SignatureTopLeftNotchTop", new Vector3(0.245f, 0.302f, 0.0f), new Vector3(0.055f, 0.050f, 0.535f), creviceDark);

        // ---------------------------------------------------------------------
        // 2. STUBBY SIDE ARMS (Left & Right protruding blocky arms)
        // ---------------------------------------------------------------------
        GameObject lArmPivotObj = new GameObject("LeftArmPivot");
        lArmPivotObj.transform.SetParent(bodyGroup, false);
        lArmPivotObj.transform.localPosition = new Vector3(-0.48f, -0.03f, 0f);
        leftArmPivot = lArmPivotObj.transform;
        MakeBox(leftArmPivot, "LeftArmBlock", new Vector3(-0.11f, 0f, 0f), new Vector3(0.22f, 0.22f, 0.25f), sideShadeOrange);
        MakeBox(leftArmPivot, "LeftArmTopCap", new Vector3(-0.11f, 0.108f, 0f), new Vector3(0.19f, 0.024f, 0.21f), topBevelOrange);
        MakeBox(leftArmPivot, "LeftArmFrontCap", new Vector3(-0.11f, 0f, 0.122f), new Vector3(0.18f, 0.17f, 0.026f), frontFaceOrange);

        GameObject rArmPivotObj = new GameObject("RightArmPivot");
        rArmPivotObj.transform.SetParent(bodyGroup, false);
        rArmPivotObj.transform.localPosition = new Vector3(0.48f, -0.03f, 0f);
        rightArmPivot = rArmPivotObj.transform;
        MakeBox(rightArmPivot, "RightArmBlock", new Vector3(0.11f, 0f, 0f), new Vector3(0.22f, 0.22f, 0.25f), mainOrange);
        MakeBox(rightArmPivot, "RightArmTopCap", new Vector3(0.11f, 0.108f, 0f), new Vector3(0.19f, 0.024f, 0.21f), topBevelOrange);
        MakeBox(rightArmPivot, "RightArmFrontCap", new Vector3(0.11f, 0f, 0.122f), new Vector3(0.18f, 0.17f, 0.026f), frontFaceOrange);

        // ---------------------------------------------------------------------
        // 3. BLOCKY LEGS (4 Front + 4 Back arranged in Left Pair & Right Pair)
        // ---------------------------------------------------------------------
        float[] legX = { -0.35f, -0.17f, 0.17f, 0.35f };
        float[] legZ = { 0.14f, -0.14f }; // Front row (+Z) & Back row (-Z)
        for (int row = 0; row < legZ.Length; row++)
        {
            for (int col = 0; col < legX.Length; col++)
            {
                GameObject legPivotObj = new GameObject($"LegPivot_R{row}_C{col}");
                legPivotObj.transform.SetParent(mascotPivot, false);
                legPivotObj.transform.localPosition = new Vector3(legX[col], 0.27f, legZ[row]);

                Material legMat = (row == 0) ? mainOrange : backLegOrange;
                Material frontTrim = (row == 0) ? frontFaceOrange : sideShadeOrange;
                MakeBox(legPivotObj.transform, "LegPillar", new Vector3(0f, -0.135f, 0f), new Vector3(0.13f, 0.27f, 0.14f), legMat);
                MakeBox(legPivotObj.transform, "LegFrontHighlight", new Vector3(0f, -0.135f, 0.068f), new Vector3(0.105f, 0.24f, 0.018f), frontTrim);
                MakeBox(legPivotObj.transform, "LegFootBevel", new Vector3(0f, -0.268f, 0f), new Vector3(0.11f, 0.024f, 0.12f), backLegOrange);

                legPivots.Add(legPivotObj.transform);
            }
        }

        // ---------------------------------------------------------------------
        // 4. EXPRESSIVE 3D FACE (Front Face at Z = +0.280f)
        // ---------------------------------------------------------------------
        GameObject faceRoot = new GameObject("FaceRoot");
        faceRoot.transform.SetParent(bodyGroup, false);
        faceRoot.transform.localPosition = new Vector3(0f, 0.01f, 0.280f);

        // A. Normal Vertical Eyes '|   |' (Image 1)
        GameObject vertRoot = new GameObject("VerticalEyesRoot");
        vertRoot.transform.SetParent(faceRoot.transform, false);
        verticalEyesRoot = vertRoot.transform;

        // Recessed sockets
        MakeBox(verticalEyesRoot, "LeftEyeSocket", new Vector3(-0.245f, 0f, -0.004f), new Vector3(0.135f, 0.245f, 0.016f), creviceDark);
        MakeBox(verticalEyesRoot, "RightEyeSocket", new Vector3(0.245f, 0f, -0.004f), new Vector3(0.135f, 0.245f, 0.016f), creviceDark);

        leftVerticalEye = MakeBox(verticalEyesRoot, "LeftEyeBar", new Vector3(-0.245f, 0f, 0.008f), new Vector3(0.112f, 0.225f, 0.030f), eyeBlack).transform;
        rightVerticalEye = MakeBox(verticalEyesRoot, "RightEyeBar", new Vector3(0.245f, 0f, 0.008f), new Vector3(0.112f, 0.225f, 0.030f), eyeBlack).transform;

        // B. Chevron Eyes '>   <' (Image 2)
        GameObject chevRoot = new GameObject("ChevronEyesRoot");
        chevRoot.transform.SetParent(faceRoot.transform, false);
        chevronEyesRoot = chevRoot.transform;

        // Left '>' Chevron (points right toward center)
        CreateChevronEye(chevronEyesRoot, "LeftChevron", new Vector3(-0.235f, 0f, 0.012f), pointsRight: true, eyeBlack);
        // Right '<' Chevron (points left toward center)
        CreateChevronEye(chevronEyesRoot, "RightChevron", new Vector3(0.235f, 0f, 0.012f), pointsRight: false, eyeBlack);

        // C. Happy Arc Eyes '^   ^' (Victory state)
        GameObject hapRoot = new GameObject("HappyEyesRoot");
        hapRoot.transform.SetParent(faceRoot.transform, false);
        happyEyesRoot = hapRoot.transform;
        CreateHappyArchEye(happyEyesRoot, "LeftHappyEye", new Vector3(-0.235f, 0.02f, 0.012f), eyeBlack);
        CreateHappyArchEye(happyEyesRoot, "RightHappyEye", new Vector3(0.235f, 0.02f, 0.012f), eyeBlack);

        // D. Dizzy Cross Eyes 'X   X' (Dead/Burned state)
        GameObject dizRoot = new GameObject("DizzyEyesRoot");
        dizRoot.transform.SetParent(faceRoot.transform, false);
        dizzyEyesRoot = dizRoot.transform;
        CreateCrossEye(dizzyEyesRoot, "LeftCrossEye", new Vector3(-0.235f, 0f, 0.012f), eyeBlack);
        CreateCrossEye(dizzyEyesRoot, "RightCrossEye", new Vector3(0.235f, 0f, 0.012f), eyeBlack);

        // E. Cute Cheek Blush (shown on Happy/Victory)
        GameObject blRoot = new GameObject("BlushRoot");
        blRoot.transform.SetParent(faceRoot.transform, false);
        blushRoot = blRoot.transform;
        MakeBox(blushRoot, "LeftBlush", new Vector3(-0.34f, -0.14f, 0.008f), new Vector3(0.10f, 0.050f, 0.020f), blushPink);
        MakeBox(blushRoot, "RightBlush", new Vector3(0.34f, -0.14f, 0.008f), new Vector3(0.10f, 0.050f, 0.020f), blushPink);

        SetExpression(EyeExpression.NormalVertical);
    }

    private static void CreateChevronEye(Transform parent, string name, Vector3 centerPos, bool pointsRight, Material mat)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = centerPos;

        float dir = pointsRight ? 1f : -1f;
        // Upper arm of '>' or '<'
        var upper = MakeBox(root.transform, "UpperBar", new Vector3(0f, 0.042f, 0f), new Vector3(0.21f, 0.056f, 0.036f), mat);
        upper.transform.localRotation = Quaternion.Euler(0f, 0f, -25f * dir);

        // Lower arm of '>' or '<'
        var lower = MakeBox(root.transform, "LowerBar", new Vector3(0f, -0.042f, 0f), new Vector3(0.21f, 0.056f, 0.036f), mat);
        lower.transform.localRotation = Quaternion.Euler(0f, 0f, 25f * dir);

        // Sharp apex cap where the two bars meet
        MakeBox(root.transform, "ApexCap", new Vector3(dir * 0.076f, 0f, 0f), new Vector3(0.056f, 0.060f, 0.036f), mat);
    }

    private static void CreateHappyArchEye(Transform parent, string name, Vector3 centerPos, Material mat)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = centerPos;

        var leftSide = MakeBox(root.transform, "LeftSlope", new Vector3(-0.046f, 0f, 0f), new Vector3(0.135f, 0.052f, 0.034f), mat);
        leftSide.transform.localRotation = Quaternion.Euler(0f, 0f, 32f);

        var rightSide = MakeBox(root.transform, "RightSlope", new Vector3(0.046f, 0f, 0f), new Vector3(0.135f, 0.052f, 0.034f), mat);
        rightSide.transform.localRotation = Quaternion.Euler(0f, 0f, -32f);

        MakeBox(root.transform, "TopPeak", new Vector3(0f, 0.036f, 0f), new Vector3(0.062f, 0.052f, 0.034f), mat);
    }

    private static void CreateCrossEye(Transform parent, string name, Vector3 centerPos, Material mat)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = centerPos;

        var barA = MakeBox(root.transform, "DiagA", Vector3.zero, new Vector3(0.21f, 0.050f, 0.034f), mat);
        barA.transform.localRotation = Quaternion.Euler(0f, 0f, 42f);

        var barB = MakeBox(root.transform, "DiagB", Vector3.zero, new Vector3(0.21f, 0.050f, 0.034f), mat);
        barB.transform.localRotation = Quaternion.Euler(0f, 0f, -42f);
    }

    private static GameObject MakeBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = localScale;
        cube.GetComponent<Renderer>().sharedMaterial = mat;
        var col = cube.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        return cube;
    }

    // =========================================================================
    // Public Action Triggers Called by PlayerController
    // =========================================================================

    public void NotifyWalkStart(Vector2Int dir, float duration)
    {
        walkDirection = dir;
        walkDuration = Mathf.Max(0.08f, duration);
        walkTimer = walkDuration;
    }

    public void NotifyJumpStart(float duration)
    {
        jumpDuration = Mathf.Max(0.1f, duration);
        jumpTimer = jumpDuration;
    }

    public void NotifyBumpStart(Vector2Int dir, float duration)
    {
        bumpDirection = dir;
        bumpDuration = Mathf.Max(0.08f, duration);
        bumpTimer = bumpDuration;
    }

    public void NotifyPulseStart(float duration)
    {
        pulseDuration = Mathf.Max(0.1f, duration);
        pulseTimer = pulseDuration;
    }

    public void ResetToIdle()
    {
        walkTimer = 0f;
        jumpTimer = 0f;
        bumpTimer = 0f;
        pulseTimer = 0f;
        landSquashTimer = 0f;
        SetExpression(EyeExpression.NormalVertical);
    }

    public void SetExpression(EyeExpression expr)
    {
        currentExpression = expr;
        if (verticalEyesRoot != null) verticalEyesRoot.gameObject.SetActive(expr == EyeExpression.NormalVertical);
        if (chevronEyesRoot != null) chevronEyesRoot.gameObject.SetActive(expr == EyeExpression.ChevronSqueeze);
        if (happyEyesRoot != null) happyEyesRoot.gameObject.SetActive(expr == EyeExpression.HappyArcs);
        if (dizzyEyesRoot != null) dizzyEyesRoot.gameObject.SetActive(expr == EyeExpression.DizzyCross);
        if (blushRoot != null) blushRoot.gameObject.SetActive(expr == EyeExpression.HappyArcs);
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (mascotPivot == null || bodyGroup == null) return;

        if (player == null) player = GetComponentInParent<PlayerController>();

        float dt = Time.deltaTime;
        float tNow = Time.time;

        // Tick active action timers
        bool wasJumping = jumpTimer > 0f;
        if (walkTimer > 0f) walkTimer = Mathf.Max(0f, walkTimer - dt);
        if (jumpTimer > 0f) jumpTimer = Mathf.Max(0f, jumpTimer - dt);
        if (bumpTimer > 0f) bumpTimer = Mathf.Max(0f, bumpTimer - dt);
        if (pulseTimer > 0f) pulseTimer = Mathf.Max(0f, pulseTimer - dt);
        if (landSquashTimer > 0f) landSquashTimer = Mathf.Max(0f, landSquashTimer - dt);

        if (wasJumping && jumpTimer <= 0f)
        {
            landSquashTimer = 0.16f;
        }

        PlayerState pState = player != null ? player.currentState : PlayerState.Idle;

        // ---------------------------------------------------------------------
        // 1. Determine Facial Expression & Body Pose
        // ---------------------------------------------------------------------
        Vector3 bodyPosOffset = Vector3.zero;
        Vector3 bodyScale = Vector3.one;
        Vector3 bodyEuler = Vector3.zero;
        float leftArmAngle = 0f;
        float rightArmAngle = 0f;
        float targetYaw = BasePresentationYaw;

        if (pState == PlayerState.Dead)
        {
            // Hurt / Knocked out in lava or spikes: '>  <' briefly then 'X  X' + singed hop
            SetExpression(Mathf.Sin(tNow * 28f) > 0.2f ? EyeExpression.ChevronSqueeze : EyeExpression.DizzyCross);
            float hop = Mathf.Abs(Mathf.Sin(tNow * 14f)) * 0.24f;
            bodyPosOffset.y = hop;
            bodyEuler = new Vector3(-12f, Mathf.Sin(tNow * 25f) * 24f, Mathf.Cos(tNow * 22f) * 14f);
            bodyScale = new Vector3(0.92f, 1.12f, 0.92f);
            leftArmAngle = 55f + Mathf.Sin(tNow * 35f) * 25f;
            rightArmAngle = -55f - Mathf.Sin(tNow * 35f) * 25f;
            AnimateLegsFlail(tNow);
        }
        else if (pState == PlayerState.Win)
        {
            // Victory Dance! Happy '^  ^' eyes (with occasional '>  <' on big cheers)
            float dancePhase = tNow * 7.5f;
            float hop = Mathf.Abs(Mathf.Sin(dancePhase));
            SetExpression(hop > 0.88f ? EyeExpression.ChevronSqueeze : EyeExpression.HappyArcs);

            bodyPosOffset.y = hop * 0.20f;
            float squash = Mathf.Cos(dancePhase * 2f) * 0.08f;
            bodyScale = new Vector3(1f - squash, 1f + squash, 1f - squash);
            bodyEuler = new Vector3(0f, Mathf.Sin(dancePhase * 0.5f) * 16f, Mathf.Sin(dancePhase) * 9f);

            leftArmAngle = 42f + Mathf.Sin(dancePhase * 2f) * 28f;
            rightArmAngle = -42f - Mathf.Sin(dancePhase * 2f) * 28f;
            AnimateLegsDance(dancePhase);
        }
        else if (jumpTimer > 0f)
        {
            // JUMP: Iconic '>  <' chevron eyes from Image 2!
            SetExpression(EyeExpression.ChevronSqueeze);
            float u = 1f - (jumpTimer / jumpDuration); // 0 -> 1

            if (u < 0.18f)
            {
                // Launch stretch
                float p = u / 0.18f;
                bodyScale = Vector3.Lerp(new Vector3(1.16f, 0.82f, 1.12f), new Vector3(0.86f, 1.20f, 0.88f), p);
            }
            else if (u < 0.82f)
            {
                // Midair vault
                float airPhase = (u - 0.18f) / 0.64f;
                bodyScale = Vector3.Lerp(new Vector3(0.88f, 1.16f, 0.90f), new Vector3(0.95f, 1.06f, 0.95f), Mathf.Sin(airPhase * Mathf.PI));
                bodyEuler.z = Mathf.Sin(airPhase * Mathf.PI) * -8f;
            }
            else
            {
                // Pre-landing compress
                float p = (u - 0.82f) / 0.18f;
                bodyScale = Vector3.Lerp(Vector3.one, new Vector3(1.18f, 0.80f, 1.14f), p);
            }

            // Arms flung up in excitement during jump!
            leftArmAngle = 48f;
            rightArmAngle = -48f;
            targetYaw = BasePresentationYaw - 8f;
            AnimateLegsJump(u);
        }
        else if (pulseTimer > 0f)
        {
            // EMP PULSE: '>  <' chevron eyes + energetic pulse swell
            SetExpression(EyeExpression.ChevronSqueeze);
            float u = 1f - (pulseTimer / pulseDuration);
            float swell = Mathf.Sin(u * Mathf.PI) * 0.15f;
            bodyScale = new Vector3(1f + swell, 1f - swell * 0.5f, 1f + swell);
            leftArmAngle = 38f;
            rightArmAngle = -38f;
            AnimateLegsJump(u);
        }
        else if (bumpTimer > 0f)
        {
            // BUMPED INTO WALL: '>  <' chevron squeeze + head shake
            SetExpression(EyeExpression.ChevronSqueeze);
            float u = 1f - (bumpTimer / bumpDuration);
            float shake = Mathf.Sin(u * Mathf.PI * 4f) * (1f - u) * 18f;
            bodyEuler = new Vector3(0f, shake, shake * 0.4f);
            float smush = Mathf.Sin(u * Mathf.PI) * 0.14f;
            bodyScale = new Vector3(1f + smush, 1f - smush * 0.7f, 1f + smush);
            leftArmAngle = 32f * Mathf.Sin(u * Mathf.PI * 3f);
            rightArmAngle = -32f * Mathf.Sin(u * Mathf.PI * 3f);
            AnimateLegsIdle(tNow);
        }
        else if (walkTimer > 0f)
        {
            // WALK / SIDESTEP: Vertical '|  |' eyes looking in step direction + 8-leg crab scuttle!
            SetExpression(EyeExpression.NormalVertical);
            float u = 1f - (walkTimer / walkDuration); // 0 -> 1

            // Double-step bounce per tile
            float stepBounce = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 2f));
            bodyPosOffset.y = stepBounce * 0.065f;

            // Subtle waddle rock & squash-stretch
            float rock = Mathf.Sin(u * Mathf.PI * 2f);
            bodyEuler = new Vector3(4f, rock * 5f, rock * 6.5f);
            float stretch = (stepBounce - 0.5f) * 0.08f;
            bodyScale = new Vector3(1f - stretch * 0.6f, 1f + stretch, 1f - stretch * 0.6f);

            // Swing stubby arms opposite to each other
            leftArmAngle = 15f + rock * 24f;
            rightArmAngle = -15f + rock * 24f;

            // Turn presentation yaw slightly toward walk direction while keeping face visible to camera
            if (walkDirection.y > 0) targetYaw = BasePresentationYaw - 16f;      // Sidestep Left (+Z)
            else if (walkDirection.y < 0) targetYaw = BasePresentationYaw + 16f; // Sidestep Right (-Z)
            else targetYaw = BasePresentationYaw - 5f;                           // Forward (+X)

            // Gaze shifts toward movement direction
            targetGazeOffset = new Vector2(
                walkDirection.y < 0 ? -0.025f : (walkDirection.y > 0 ? 0.018f : 0.026f),
                0.008f);

            AnimateLegsWalk(u);
        }
        else
        {
            // IDLE (with optional post-landing squash recovery)
            SetExpression(EyeExpression.NormalVertical);

            float breath = Mathf.Sin(tNow * 2.8f);
            bodyPosOffset.y = breath * 0.016f;
            bodyScale = new Vector3(1f - breath * 0.014f, 1f + breath * 0.018f, 1f - breath * 0.014f);

            if (landSquashTimer > 0f)
            {
                float s = Mathf.Sin((landSquashTimer / 0.16f) * Mathf.PI) * 0.15f;
                bodyScale = new Vector3(1f + s, 1f - s, 1f + s);
            }

            bodyEuler = new Vector3(0f, Mathf.Sin(tNow * 1.1f) * 2.5f, Mathf.Cos(tNow * 1.4f) * 1.4f);
            leftArmAngle = Mathf.Sin(tNow * 2.8f - 0.5f) * 6f;
            rightArmAngle = -Mathf.Sin(tNow * 2.8f - 0.5f) * 6f;

            AnimateLegsIdle(tNow);
            UpdateIdleGaze(tNow);
        }

        // Apply smoothed presentation yaw & scale
        currentPresentationYaw = Mathf.LerpAngle(currentPresentationYaw, targetYaw, dt * 10f);
        spinOffset = pulseTimer > 0f ? (1f - pulseTimer / pulseDuration) * 360f : 0f;
        ApplyPivotRotation(currentPresentationYaw + spinOffset);
        mascotPivot.localScale = Vector3.one * MascotOverallScale;

        // Apply body transform
        bodyGroup.localPosition = baseBodyLocalPos + bodyPosOffset;
        bodyGroup.localScale = bodyScale;
        bodyGroup.localRotation = Quaternion.Euler(bodyEuler);

        // Apply stubby side arm rotations
        if (leftArmPivot != null)
            leftArmPivot.localRotation = Quaternion.Euler(0f, 0f, -leftArmAngle);
        if (rightArmPivot != null)
            rightArmPivot.localRotation = Quaternion.Euler(0f, 0f, -rightArmAngle);

        // Update vertical eye blinking & gaze when in NormalVertical mode
        if (currentExpression == EyeExpression.NormalVertical)
        {
            UpdateBlinkAndGaze(dt, tNow);
        }
    }

    // =========================================================================
    // Leg Animation Helpers (8 Blocky Legs: 4 Front + 4 Back)
    // =========================================================================

    private void AnimateLegsWalk(float u)
    {
        float cycle = u * Mathf.PI * 4f; // 2 full stride cycles per tile step
        for (int i = 0; i < legPivots.Count; i++)
        {
            Transform leg = legPivots[i];
            if (leg == null) continue;

            // Alternating phase across the 4 columns and 2 rows (octo/crab scuttle wave!)
            float phase = cycle + (i % 4) * (Mathf.PI * 0.55f) + (i / 4) * Mathf.PI;
            float swing = Mathf.Sin(phase) * 28f;
            float splay = Mathf.Cos(phase) * 10f;
            float lift = Mathf.Max(0f, Mathf.Cos(phase)) * 0.045f;

            Vector3 basePos = GetBaseLegPos(i);
            leg.localPosition = basePos + new Vector3(0f, lift, 0f);
            leg.localRotation = Quaternion.Euler(swing, 0f, splay);
        }
    }

    private void AnimateLegsJump(float u)
    {
        // Splay & tuck legs in midair
        float tuck = Mathf.Sin(u * Mathf.PI);
        for (int i = 0; i < legPivots.Count; i++)
        {
            Transform leg = legPivots[i];
            if (leg == null) continue;

            int col = i % 4;
            int row = i / 4;
            float sideSign = (col < 2) ? -1f : 1f;
            float rowSign = (row == 0) ? 1f : -1f;

            Vector3 basePos = GetBaseLegPos(i);
            leg.localPosition = basePos + new Vector3(0f, tuck * 0.055f, 0f);
            leg.localRotation = Quaternion.Euler(rowSign * tuck * 22f, 0f, sideSign * tuck * 18f);
        }
    }

    private void AnimateLegsDance(float dancePhase)
    {
        for (int i = 0; i < legPivots.Count; i++)
        {
            Transform leg = legPivots[i];
            if (leg == null) continue;

            float wave = Mathf.Sin(dancePhase * 2f + i * 0.8f);
            Vector3 basePos = GetBaseLegPos(i);
            leg.localPosition = basePos + new Vector3(0f, Mathf.Max(0f, wave) * 0.04f, 0f);
            leg.localRotation = Quaternion.Euler(wave * 18f, 0f, wave * 12f);
        }
    }

    private void AnimateLegsFlail(float tNow)
    {
        for (int i = 0; i < legPivots.Count; i++)
        {
            Transform leg = legPivots[i];
            if (leg == null) continue;

            float wave = Mathf.Sin(tNow * 32f + i * 1.3f);
            leg.localPosition = GetBaseLegPos(i);
            leg.localRotation = Quaternion.Euler(wave * 32f, 0f, Mathf.Cos(tNow * 28f + i) * 20f);
        }
    }

    private void AnimateLegsIdle(float tNow)
    {
        for (int i = 0; i < legPivots.Count; i++)
        {
            Transform leg = legPivots[i];
            if (leg == null) continue;

            leg.localPosition = GetBaseLegPos(i);
            leg.localRotation = Quaternion.Lerp(leg.localRotation, Quaternion.identity, Time.deltaTime * 12f);
        }
    }

    private static Vector3 GetBaseLegPos(int index)
    {
        float[] legX = { -0.35f, -0.17f, 0.17f, 0.35f };
        float[] legZ = { 0.14f, -0.14f };
        int col = index % 4;
        int row = index / 4;
        return new Vector3(legX[col], 0.27f, legZ[row]);
    }

    // =========================================================================
    // Blink & Eye Gaze Animation
    // =========================================================================

    private void UpdateIdleGaze(float tNow)
    {
        if (tNow >= nextGazeChangeTime)
        {
            nextGazeChangeTime = tNow + Random.Range(1.6f, 3.4f);
            int pick = Random.Range(0, 5);
            if (pick == 0) targetGazeOffset = new Vector2(0.020f, 0.006f);
            else if (pick == 1) targetGazeOffset = new Vector2(-0.020f, 0.004f);
            else if (pick == 2) targetGazeOffset = new Vector2(0f, 0.012f);
            else targetGazeOffset = Vector2.zero;
        }
    }

    private void UpdateBlinkAndGaze(float dt, float tNow)
    {
        if (leftVerticalEye == null || rightVerticalEye == null) return;

        // Smooth gaze interpolation
        currentGazeOffset = Vector2.Lerp(currentGazeOffset, targetGazeOffset, dt * 10f);
        leftVerticalEye.localPosition = new Vector3(-0.245f + currentGazeOffset.x, currentGazeOffset.y, 0.008f);
        rightVerticalEye.localPosition = new Vector3(0.245f + currentGazeOffset.x, currentGazeOffset.y, 0.008f);

        // Periodic blink trigger
        if (blinkTimer < 0f && tNow >= nextBlinkTime)
        {
            blinkTimer = 0f;
            // Occasional cute double-blink!
            nextBlinkTime = tNow + (Random.value < 0.22f ? 0.24f : Random.Range(2.2f, 4.2f));
        }

        float eyeHeightScale = 0.225f;
        float eyeWidthScale = 0.112f;
        if (blinkTimer >= 0f)
        {
            blinkTimer += dt;
            float b = blinkTimer / BlinkDuration;
            if (b >= 1f)
            {
                blinkTimer = -1f;
            }
            else
            {
                float closedFactor = Mathf.Sin(b * Mathf.PI);
                eyeHeightScale = Mathf.Lerp(0.225f, 0.020f, closedFactor);
                eyeWidthScale = Mathf.Lerp(0.112f, 0.130f, closedFactor);
            }
        }

        leftVerticalEye.localScale = new Vector3(eyeWidthScale, eyeHeightScale, 0.030f);
        rightVerticalEye.localScale = new Vector3(eyeWidthScale, eyeHeightScale, 0.030f);
    }
}
