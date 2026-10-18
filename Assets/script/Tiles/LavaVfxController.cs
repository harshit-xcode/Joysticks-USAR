// LavaVfxController.cs
// Transforms each LavaTile into a dynamic 3D volcanic magma cauldron with:
// 1. Charred basalt containment rim & incandescent magma fissures
// 2. Flowing 2-layer procedural molten lava surface (scrolling UVs + thermal pulse)
// 3. Floating dark obsidian crust islands bobbing on lava currents
// 4. Swelling & popping 3D molten lava blisters
// 5. Multi-stage 3D Particle Simulation (works in both Edit Mode, Play Mode & Standalone Builds!):
//    - Stage A: Roaring Fire Plumes & Flame Tongues (Yellow-White -> Orange -> Deep Crimson)
//    - Stage B: Crackling Rising Embers & Sparks (Streaking golden-orange cinders swirling high)
//    - Stage C: Rising Volcanic Ashes & Soot Flakes (Glowing ember-rimmed ash cooling into dark tumbling soot flakes)
// 6. Warm flickering magma point light

using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LavaVfxController : MonoBehaviour
{
    private static Texture2D sharedMagmaTex;

    [Header("Animated Surface References")]
    public Renderer primaryMagmaRenderer;
    public Renderer secondarySurgeRenderer;
    public Light magmaLight;
    public List<Transform> floatingCrusts = new List<Transform>();
    public List<Transform> lavaBubbles = new List<Transform>();
    public List<Transform> flameTongues = new List<Transform>();
    public List<Transform> firePlumeParticles = new List<Transform>();
    public List<Transform> risingEmberMotes = new List<Transform>();
    public List<Transform> risingAshFlakes = new List<Transform>();

    // Per-instance random phase so adjacent lava tiles don't animate in lockstep
    private float seedOffset;
    private float[] bubbleTimers = new float[6];
    private Vector3[] bubblePos = new Vector3[6];
    private Vector3[] crustBasePos = new Vector3[4];

    private const int FlameCount = 9;
    private const int PlumeCount = 16;
    private const int EmberCount = 18;
    private const int AshCount = 16;

    private Vector2[] flameBaseXZ = new Vector2[FlameCount];
    private float[] plumePhases = new float[PlumeCount];
    private Vector2[] plumeBaseXZ = new Vector2[PlumeCount];
    private float[] emberPhases = new float[EmberCount];
    private Vector2[] emberBaseXZ = new Vector2[EmberCount];
    private float[] ashPhases = new float[AshCount];
    private Vector2[] ashBaseXZ = new Vector2[AshCount];

    private Material instanceMagmaMat;
    private Material instanceSurgeMat;

    public static LavaVfxController BuildOnTile(Transform tileTransform)
    {
        var vfx = tileTransform.GetComponent<LavaVfxController>();
        if (vfx == null) vfx = tileTransform.gameObject.AddComponent<LavaVfxController>();
        vfx.ConstructLavaPit();
        return vfx;
    }

    private void OnEnable()
    {
        seedOffset = (transform.localPosition.x * 1.37f + transform.localPosition.z * 2.19f);
        InitRandomStates();
        EnsureMaterialsInstanced();
    }

    private void InitRandomStates()
    {
        Random.State oldState = Random.state;
        Random.InitState(Mathf.Abs(Mathf.RoundToInt(seedOffset * 1000f)) + 73);

        for (int i = 0; i < bubbleTimers.Length; i++)
        {
            bubbleTimers[i] = Random.Range(0f, 1f);
            bubblePos[i] = new Vector3(Random.Range(-0.50f, 0.50f), 0.085f, Random.Range(-0.50f, 0.50f));
        }

        crustBasePos[0] = new Vector3(-0.34f, 0.082f, 0.28f);
        crustBasePos[1] = new Vector3(0.32f, 0.082f, -0.26f);
        crustBasePos[2] = new Vector3(0.24f, 0.082f, 0.34f);
        crustBasePos[3] = new Vector3(-0.28f, 0.082f, -0.32f);

        for (int i = 0; i < FlameCount; i++)
        {
            flameBaseXZ[i] = new Vector2(Random.Range(-0.48f, 0.48f), Random.Range(-0.48f, 0.48f));
        }

        for (int i = 0; i < PlumeCount; i++)
        {
            plumePhases[i] = (i / (float)PlumeCount) + Random.Range(0f, 0.08f);
            plumeBaseXZ[i] = new Vector2(Random.Range(-0.52f, 0.52f), Random.Range(-0.52f, 0.52f));
        }

        for (int i = 0; i < EmberCount; i++)
        {
            emberPhases[i] = (i / (float)EmberCount) + Random.Range(0f, 0.06f);
            emberBaseXZ[i] = new Vector2(Random.Range(-0.56f, 0.56f), Random.Range(-0.56f, 0.56f));
        }

        for (int i = 0; i < AshCount; i++)
        {
            ashPhases[i] = (i / (float)AshCount) + Random.Range(0f, 0.07f);
            ashBaseXZ[i] = new Vector2(Random.Range(-0.60f, 0.60f), Random.Range(-0.60f, 0.60f));
        }

        Random.state = oldState;
    }

    public void ConstructLavaPit()
    {
        seedOffset = (transform.localPosition.x * 1.37f + transform.localPosition.z * 2.19f);
        InitRandomStates();

        Transform existingRoot = transform.Find("LavaPitRoot");
        if (existingRoot != null)
            Object.DestroyImmediate(existingRoot.gameObject);

        GameObject root = new GameObject("LavaPitRoot");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;

        EnsureSharedAssets();

        Material basaltRimMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "VolcanicBasaltRim", new Color(0.11f, 0.08f, 0.08f), false, 0.18f);
        Material charredInnerMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "VolcanicCharredInner", new Color(0.24f, 0.05f, 0.02f), true, 0.25f);
        Material crustRockMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "VolcanicCrustRock", new Color(0.14f, 0.06f, 0.05f), false, 0.15f);
        Material hotFissureMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "VolcanicHotFissure", new Color(1.00f, 0.58f, 0.08f), true, 0.75f);
        Material bubbleMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "VolcanicBubbleGlow", new Color(1.00f, 0.74f, 0.16f), true, 0.85f);

        // Fire & Ash Particle Materials (Temperature Gradient Palette)
        Material fireWhiteYellowMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "FireWhiteYellow", new Color(1.00f, 0.94f, 0.42f), true, 0.85f);
        Material fireBrightOrangeMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "FireBrightOrange", new Color(1.00f, 0.46f, 0.06f), true, 0.75f);
        Material fireDeepCrimsonMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "FireDeepCrimson", new Color(0.88f, 0.16f, 0.03f), true, 0.55f);
        Material ashHotRimMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "AshHotRim", new Color(0.52f, 0.18f, 0.09f), true, 0.20f);
        Material ashCharcoalMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "AshCharcoal", new Color(0.16f, 0.12f, 0.12f), false, 0.10f);

        // ---------------------------------------------------------------------
        // 1. Charred Basalt Containment Rim & Glowing Heat Shelf
        // ---------------------------------------------------------------------
        MakeBox(root.transform, "BasaltRimN", new Vector3(0f, 0.095f, 0.73f), new Vector3(1.62f, 0.11f, 0.16f), basaltRimMat);
        MakeBox(root.transform, "BasaltRimS", new Vector3(0f, 0.095f, -0.73f), new Vector3(1.62f, 0.11f, 0.16f), basaltRimMat);
        MakeBox(root.transform, "BasaltRimE", new Vector3(0.73f, 0.095f, 0f), new Vector3(0.16f, 0.11f, 1.30f), basaltRimMat);
        MakeBox(root.transform, "BasaltRimW", new Vector3(-0.73f, 0.095f, 0f), new Vector3(0.16f, 0.11f, 1.30f), basaltRimMat);

        MakeBox(root.transform, "HeatShelfN", new Vector3(0f, 0.078f, 0.63f), new Vector3(1.32f, 0.05f, 0.05f), charredInnerMat);
        MakeBox(root.transform, "HeatShelfS", new Vector3(0f, 0.078f, -0.63f), new Vector3(1.32f, 0.05f, 0.05f), charredInnerMat);
        MakeBox(root.transform, "HeatShelfE", new Vector3(0.63f, 0.078f, 0f), new Vector3(0.05f, 0.05f, 1.24f), charredInnerMat);
        MakeBox(root.transform, "HeatShelfW", new Vector3(-0.63f, 0.078f, 0f), new Vector3(0.05f, 0.05f, 1.24f), charredInnerMat);

        // ---------------------------------------------------------------------
        // 2. Flowing Molten Magma Surface (2 Scrolling Procedural Layers)
        // ---------------------------------------------------------------------
        var primaryPool = MakeBox(root.transform, "MoltenMagmaPrimary", new Vector3(0f, 0.066f, 0f), new Vector3(1.34f, 0.035f, 1.34f), hotFissureMat);
        primaryMagmaRenderer = primaryPool.GetComponent<Renderer>();

        var surgeVeins = MakeBox(root.transform, "MoltenMagmaSurge", new Vector3(0f, 0.074f, 0f), new Vector3(1.26f, 0.024f, 1.26f), bubbleMat);
        secondarySurgeRenderer = surgeVeins.GetComponent<Renderer>();

        EnsureMaterialsInstanced();

        // ---------------------------------------------------------------------
        // 3. Floating Volcanic Obsidian Crust Islands (Bobbing & Drifting)
        // ---------------------------------------------------------------------
        floatingCrusts.Clear();
        Vector3[] crustScales =
        {
            new Vector3(0.36f, 0.045f, 0.28f),
            new Vector3(0.32f, 0.042f, 0.34f),
            new Vector3(0.24f, 0.040f, 0.22f),
            new Vector3(0.30f, 0.044f, 0.25f)
        };
        float[] crustYaws = { 18f, -34f, 52f, -15f };

        for (int i = 0; i < 4; i++)
        {
            GameObject crustGroup = new GameObject($"CrustIsland_{i}");
            crustGroup.transform.SetParent(root.transform, false);
            crustGroup.transform.localPosition = crustBasePos[i];
            crustGroup.transform.localRotation = Quaternion.Euler(0f, crustYaws[i], 0f);

            MakeBox(crustGroup.transform, "EmberUnderside", new Vector3(0f, -0.006f, 0f), crustScales[i] * 1.12f, hotFissureMat);
            MakeBox(crustGroup.transform, "ObsidianSlab", new Vector3(0f, 0.012f, 0f), crustScales[i], crustRockMat);

            floatingCrusts.Add(crustGroup.transform);
        }

        // ---------------------------------------------------------------------
        // 4. Animated Swelling & Popping Magma Bubbles
        // ---------------------------------------------------------------------
        lavaBubbles.Clear();
        for (int i = 0; i < bubbleTimers.Length; i++)
        {
            var bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bubble.name = $"MagmaBubble_{i}";
            bubble.transform.SetParent(root.transform, false);
            bubble.transform.localPosition = bubblePos[i];
            bubble.transform.localScale = new Vector3(0.14f, 0.09f, 0.14f);
            bubble.GetComponent<Renderer>().sharedMaterial = bubbleMat;
            var col = bubble.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            lavaBubbles.Add(bubble.transform);
        }

        // ---------------------------------------------------------------------
        // 5. Multi-Stage 3D Fire, Plumes, Crackling Embers & Rising Ashes
        // ---------------------------------------------------------------------
        // Stage A1: Surface Flame Tongues
        flameTongues.Clear();
        for (int i = 0; i < FlameCount; i++)
        {
            GameObject flamePivot = new GameObject($"FlameTongue_{i}");
            flamePivot.transform.SetParent(root.transform, false);
            flamePivot.transform.localPosition = new Vector3(flameBaseXZ[i].x, 0.085f, flameBaseXZ[i].y);

            var outer = MakeBox(flamePivot.transform, "OuterFlame", new Vector3(0f, 0.11f, 0f), new Vector3(0.15f, 0.24f, 0.15f), flameOuterMat(i, fireBrightOrangeMat, fireDeepCrimsonMat));
            outer.transform.localRotation = Quaternion.Euler(14f, i * 37f, 18f);
            var inner = MakeBox(flamePivot.transform, "InnerCore", new Vector3(0f, 0.07f, 0f), new Vector3(0.09f, 0.16f, 0.09f), fireWhiteYellowMat);
            inner.transform.localRotation = Quaternion.Euler(-12f, i * 53f, -15f);

            flameTongues.Add(flamePivot.transform);
        }

        // Stage A2: Billowing Fire Plume Particles (rising & shrinking flame puffs)
        firePlumeParticles.Clear();
        for (int i = 0; i < PlumeCount; i++)
        {
            Material pMat = (i % 3 == 0) ? fireWhiteYellowMat : ((i % 3 == 1) ? fireBrightOrangeMat : fireDeepCrimsonMat);
            var plume = MakeBox(root.transform, $"FirePlumeParticle_{i}", Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), pMat);
            firePlumeParticles.Add(plume.transform);
        }

        // Stage B: Crackling Rising Embers & Sparks (streaking upward in convection currents)
        risingEmberMotes.Clear();
        for (int i = 0; i < EmberCount; i++)
        {
            Material eMat = (i % 2 == 0) ? fireWhiteYellowMat : fireBrightOrangeMat;
            var ember = MakeBox(root.transform, $"RisingEmberSpark_{i}", Vector3.zero, new Vector3(0.05f, 0.10f, 0.05f), eMat);
            risingEmberMotes.Add(ember.transform);
        }

        // Stage C: Rising Volcanic Ash Flakes & Soot (drifting & tumbling high above the pit)
        risingAshFlakes.Clear();
        for (int i = 0; i < AshCount; i++)
        {
            Material aMat = (i % 4 == 0) ? ashHotRimMat : ashCharcoalMat;
            var ash = MakeBox(root.transform, $"RisingAshFlake_{i}", Vector3.zero, new Vector3(0.08f, 0.020f, 0.065f), aMat);
            risingAshFlakes.Add(ash.transform);
        }

        // ---------------------------------------------------------------------
        // 6. Warm Flickering Firelight Point Light
        // ---------------------------------------------------------------------
        GameObject lightObj = new GameObject("MagmaGlowLight");
        lightObj.transform.SetParent(root.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        magmaLight = lightObj.AddComponent<Light>();
        magmaLight.type = LightType.Point;
        magmaLight.color = new Color(1.00f, 0.42f, 0.08f);
        magmaLight.range = 2.85f;
        magmaLight.intensity = 1.25f;
        magmaLight.shadows = LightShadows.None;

        // Run one immediate visual evaluation so static scenes/screenshots look alive
        EvaluateAnimation(0.75f, 0.016f);
    }

    private static Material flameOuterMat(int idx, Material brightOrange, Material crimson)
    {
        return (idx % 2 == 0) ? brightOrange : crimson;
    }

    private void EnsureMaterialsInstanced()
    {
        EnsureSharedAssets();

        Shader unlitTex = Shader.Find("Unlit/Texture");
        if (unlitTex == null) unlitTex = Shader.Find("Standard");
        if (unlitTex == null) unlitTex = Shader.Find("Sprites/Default");

        if (primaryMagmaRenderer != null)
        {
            if (instanceMagmaMat == null)
            {
                Shader std = Shader.Find("Standard");
                instanceMagmaMat = new Material(std != null ? std : unlitTex) { name = "Mat_LavaFlowPrimary" };
                instanceMagmaMat.mainTexture = sharedMagmaTex;
                instanceMagmaMat.color = new Color(1.0f, 0.78f, 0.35f);
                instanceMagmaMat.EnableKeyword("_EMISSION");
                if (instanceMagmaMat.HasProperty("_EmissionMap"))
                    instanceMagmaMat.SetTexture("_EmissionMap", sharedMagmaTex);
                if (instanceMagmaMat.HasProperty("_EmissionColor"))
                    instanceMagmaMat.SetColor("_EmissionColor", new Color(1.0f, 0.52f, 0.12f) * 2.1f);
            }
            primaryMagmaRenderer.sharedMaterial = instanceMagmaMat;
        }

        if (secondarySurgeRenderer != null)
        {
            if (instanceSurgeMat == null)
            {
                Shader std = Shader.Find("Standard");
                instanceSurgeMat = new Material(std != null ? std : unlitTex) { name = "Mat_LavaFlowSurge" };
                instanceSurgeMat.mainTexture = sharedMagmaTex;
                instanceSurgeMat.mainTextureScale = new Vector2(1.45f, 1.45f);
                instanceSurgeMat.color = new Color(1.0f, 0.45f, 0.10f);
                instanceSurgeMat.EnableKeyword("_EMISSION");
                if (instanceSurgeMat.HasProperty("_EmissionMap"))
                    instanceSurgeMat.SetTexture("_EmissionMap", sharedMagmaTex);
                if (instanceSurgeMat.HasProperty("_EmissionColor"))
                    instanceSurgeMat.SetColor("_EmissionColor", new Color(1.0f, 0.38f, 0.05f) * 1.75f);
            }
            secondarySurgeRenderer.sharedMaterial = instanceSurgeMat;
        }
    }

    private static void EnsureSharedAssets()
    {
        if (sharedMagmaTex != null) return;

        const int size = 128;
        sharedMagmaTex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Proc_MoltenMagmaTex",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        Color darkCrust = new Color(0.18f, 0.03f, 0.02f, 1f);
        Color deepRed = new Color(0.78f, 0.11f, 0.02f, 1f);
        Color brightOrange = new Color(1.00f, 0.42f, 0.04f, 1f);
        Color hotYellow = new Color(1.00f, 0.92f, 0.38f, 1f);

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x / (float)size) * Mathf.PI * 2f;
                float v = (y / (float)size) * Mathf.PI * 2f;

                float nx = Mathf.Cos(u) * 1.3f + 2.5f;
                float ny = Mathf.Sin(u) * 1.3f + 2.5f;
                float nz = Mathf.Cos(v) * 1.3f + 2.5f;
                float nw = Mathf.Sin(v) * 1.3f + 2.5f;

                float n1 = Mathf.PerlinNoise(nx + nz, ny + nw);
                float n2 = Mathf.PerlinNoise(nx * 2.1f - nw, ny * 2.1f + nz);
                float ridge = 1f - Mathf.Abs(n2 - 0.5f) * 2f;
                float heat = Mathf.Clamp01(n1 * 0.55f + ridge * ridge * 0.65f);

                Color c;
                if (heat < 0.38f)
                    c = Color.Lerp(darkCrust, deepRed, heat / 0.38f);
                else if (heat < 0.72f)
                    c = Color.Lerp(deepRed, brightOrange, (heat - 0.38f) / 0.34f);
                else
                    c = Color.Lerp(brightOrange, hotYellow, (heat - 0.72f) / 0.28f);

                pixels[y * size + x] = c;
            }
        }
        sharedMagmaTex.SetPixels(pixels);
        sharedMagmaTex.Apply(false, false);
    }

    private void Update()
    {
        float t = (Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup) + seedOffset;
        float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
        EvaluateAnimation(t, dt);
    }

    private void EvaluateAnimation(float t, float dt)
    {
        // 1. Scroll 2-layer procedural molten magma UVs & pulse emission
        if (instanceMagmaMat != null)
        {
            Vector2 offset1 = new Vector2(t * 0.08f, Mathf.Sin(t * 0.45f) * 0.06f + t * 0.04f);
            instanceMagmaMat.mainTextureOffset = offset1;
            if (instanceMagmaMat.HasProperty("_EmissionColor"))
            {
                float pulse = 1.85f + Mathf.Sin(t * 2.8f) * 0.45f;
                instanceMagmaMat.SetColor("_EmissionColor", new Color(1.0f, 0.50f, 0.10f) * pulse);
            }
        }

        if (instanceSurgeMat != null)
        {
            Vector2 offset2 = new Vector2(-t * 0.06f + Mathf.Cos(t * 0.5f) * 0.05f, -t * 0.09f);
            instanceSurgeMat.mainTextureOffset = offset2;
            if (instanceSurgeMat.HasProperty("_EmissionColor"))
            {
                float pulse2 = 1.55f + Mathf.Cos(t * 3.4f) * 0.40f;
                instanceSurgeMat.SetColor("_EmissionColor", new Color(1.0f, 0.38f, 0.05f) * pulse2);
            }
        }

        // 2. Animate floating obsidian crust islands
        for (int i = 0; i < floatingCrusts.Count && i < crustBasePos.Length; i++)
        {
            Transform crust = floatingCrusts[i];
            if (crust == null) continue;

            float phase = t * 1.6f + i * 1.7f;
            Vector3 drift = new Vector3(
                Mathf.Sin(phase * 0.7f) * 0.035f,
                Mathf.Sin(phase * 1.3f) * 0.008f,
                Mathf.Cos(phase * 0.6f) * 0.035f);
            crust.localPosition = crustBasePos[i] + drift;
            crust.localRotation = Quaternion.Euler(
                Mathf.Sin(phase) * 4.5f,
                (i * 42f) + Mathf.Cos(phase * 0.5f) * 6f,
                Mathf.Cos(phase * 1.1f) * 4.5f);
        }

        // 3. Animate swelling & popping molten lava bubbles
        for (int i = 0; i < lavaBubbles.Count && i < bubbleTimers.Length; i++)
        {
            Transform b = lavaBubbles[i];
            if (b == null) continue;

            bubbleTimers[i] += dt * (0.55f + i * 0.12f);
            if (bubbleTimers[i] >= 1f)
            {
                bubbleTimers[i] -= 1f;
                float angle = (t * 2.3f + i * 1.9f);
                float rad = 0.18f + (i % 3) * 0.12f;
                bubblePos[i] = new Vector3(Mathf.Cos(angle) * rad, 0.082f, Mathf.Sin(angle) * rad);
            }

            float u = bubbleTimers[i];
            float scaleFactor = (u < 0.88f)
                ? Mathf.SmoothStep(0.02f, 1.0f, u / 0.88f)
                : Mathf.Lerp(1.25f, 0.01f, (u - 0.88f) / 0.12f);

            b.localPosition = bubblePos[i] + new Vector3(0f, scaleFactor * 0.025f, 0f);
            b.localScale = new Vector3(0.22f * scaleFactor, 0.14f * scaleFactor, 0.22f * scaleFactor);
        }

        // 4. Animate Surface Flame Tongues (flickering & licking upward)
        for (int i = 0; i < flameTongues.Count && i < flameBaseXZ.Length; i++)
        {
            Transform f = flameTongues[i];
            if (f == null) continue;

            float wave = Mathf.Sin(t * (6.5f + i * 0.7f) + i * 1.4f);
            float heightScale = 0.75f + (wave * 0.5f + 0.5f) * 0.85f;
            float widthScale = 1.05f - (heightScale - 0.75f) * 0.30f;

            f.localPosition = new Vector3(
                flameBaseXZ[i].x + Mathf.Sin(t * 3.2f + i) * 0.03f,
                0.08f,
                flameBaseXZ[i].y + Mathf.Cos(t * 2.9f + i) * 0.03f);
            f.localScale = new Vector3(widthScale, heightScale, widthScale);
            f.localRotation = Quaternion.Euler(
                Mathf.Sin(t * 5.0f + i) * 14f,
                t * 45f + i * 51f,
                Mathf.Cos(t * 4.7f + i) * 14f);
        }

        // 5. Animate Billowing Fire Plume Particles (rising & tapering flame puffs)
        for (int i = 0; i < firePlumeParticles.Count && i < plumePhases.Length; i++)
        {
            Transform p = firePlumeParticles[i];
            if (p == null) continue;

            float life = Mathf.Repeat(plumePhases[i] + t * (0.85f + (i % 4) * 0.15f), 1f);
            float y = Mathf.Lerp(0.09f, 0.62f, life);
            float curl = t * 4.2f + i * 1.5f + life * 3.0f;
            float radius = 0.06f * life;

            p.localPosition = new Vector3(
                plumeBaseXZ[i].x + Mathf.Sin(curl) * radius,
                y,
                plumeBaseXZ[i].y + Mathf.Cos(curl) * radius);

            // Swell near base, taper to a sharp tip at top
            float sizeProfile = (life < 0.25f)
                ? Mathf.Lerp(0.4f, 1.0f, life / 0.25f)
                : Mathf.Lerp(1.0f, 0.05f, (life - 0.25f) / 0.75f);

            float s = 0.18f * sizeProfile;
            p.localScale = new Vector3(s, s * 1.35f, s);
            p.localRotation = Quaternion.Euler(curl * 40f, curl * 55f, curl * 30f);
        }

        // 6. Animate Crackling Rising Embers / Sparks
        for (int i = 0; i < risingEmberMotes.Count && i < emberPhases.Length; i++)
        {
            Transform e = risingEmberMotes[i];
            if (e == null) continue;

            float life = Mathf.Repeat(emberPhases[i] + t * (0.52f + (i % 5) * 0.11f), 1f);
            float y = Mathf.Lerp(0.10f, 1.45f, life);
            float swirl = t * 3.2f + i * 1.3f + life * 5.0f;
            float rad = 0.10f * Mathf.Sin(life * Mathf.PI);

            e.localPosition = new Vector3(
                emberBaseXZ[i].x + Mathf.Cos(swirl) * rad,
                y,
                emberBaseXZ[i].y + Mathf.Sin(swirl) * rad);

            float s = Mathf.Sin(life * Mathf.PI);
            e.localScale = new Vector3(0.050f * s, 0.105f * s, 0.050f * s);
            e.localRotation = Quaternion.Euler(Mathf.Sin(swirl) * 25f, swirl * 50f, Mathf.Cos(swirl) * 25f);
        }

        // 7. Animate Rising Volcanic Ash Flakes & Soot
        for (int i = 0; i < risingAshFlakes.Count && i < ashPhases.Length; i++)
        {
            Transform a = risingAshFlakes[i];
            if (a == null) continue;

            float life = Mathf.Repeat(ashPhases[i] + t * (0.30f + (i % 4) * 0.07f), 1f);
            float y = Mathf.Lerp(0.12f, 1.80f, life);
            float driftPhase = t * 1.8f + i * 2.1f + life * 3.5f;

            a.localPosition = new Vector3(
                ashBaseXZ[i].x + Mathf.Sin(driftPhase) * 0.16f * life,
                y,
                ashBaseXZ[i].y + Mathf.Cos(driftPhase * 0.8f) * 0.16f * life);

            float fadeScale = Mathf.Sin(life * Mathf.PI);
            a.localScale = new Vector3(0.088f * fadeScale, 0.020f * fadeScale, 0.072f * fadeScale);
            a.localRotation = Quaternion.Euler(driftPhase * 65f, driftPhase * 85f, driftPhase * 45f);
        }

        // 8. Flicker warm firelight Point Light
        if (magmaLight != null)
        {
            float noise = Mathf.PerlinNoise(t * 4.5f, seedOffset);
            magmaLight.intensity = Mathf.Lerp(0.95f, 1.65f, noise);
        }
    }

    private void OnDestroy()
    {
        if (instanceMagmaMat != null)
        {
            if (Application.isPlaying) Destroy(instanceMagmaMat);
            else DestroyImmediate(instanceMagmaMat);
        }
        if (instanceSurgeMat != null)
        {
            if (Application.isPlaying) Destroy(instanceSurgeMat);
            else DestroyImmediate(instanceSurgeMat);
        }
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
}
