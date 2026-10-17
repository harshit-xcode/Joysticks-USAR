// IsometricBoardBuilder.cs
// Builds and styles the 3D isometric board, tiles, obstacles, goal gate, backdrop, and robot visuals.
// Supports custom 3D prefabs via LevelManager while providing polished built-in 3D industrial geometry.

using System.Collections.Generic;
using UnityEngine;

public static class IsometricBoardBuilder
{
    private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

    public static Material GetOrCreateMaterial(string key, Color color, bool emissive = false, float smoothness = 0.35f)
    {
        string cacheKey = $"{key}_{ColorUtility.ToHtmlStringRGBA(color)}_{emissive}";
        if (materialCache.TryGetValue(cacheKey, out Material existing) && existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var mat = new Material(shader) { name = "Mat_" + key };
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", color * 1.35f);
        }

        materialCache[cacheKey] = mat;
        return mat;
    }

    public static void BuildLevelOnBoard(
        Transform boardRoot,
        LevelDefinition level,
        float cellSpacing,
        GameObject customStartPrefab = null,
        GameObject customFreePrefab = null,
        GameObject customLavaPrefab = null,
        GameObject customObstaclePrefab = null,
        GameObject customEndPrefab = null)
    {
        if (boardRoot == null || level == null) return;

        // Clear existing children under boardRoot
        for (int i = boardRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = boardRoot.GetChild(i).gameObject;
            if (Application.isPlaying)
                Object.DestroyImmediate(child);
            else
                Object.DestroyImmediate(child);
        }

        Material darkChassis = GetOrCreateMaterial("DarkChassis", new Color(0.08f, 0.08f, 0.10f), false, 0.25f);
        Material tileTop = GetOrCreateMaterial("TileTop", new Color(0.16f, 0.17f, 0.21f), false, 0.40f);
        Material startGlow = GetOrCreateMaterial("StartGlow", new Color(0.15f, 0.75f, 1.00f), true, 0.7f);
        Material endGlow = GetOrCreateMaterial("EndGlow", new Color(0.22f, 0.95f, 0.55f), true, 0.7f);
        Material lavaGlow = GetOrCreateMaterial("LavaGlow", new Color(0.92f, 0.25f, 0.18f), true, 0.5f);
        Material spikeRed = GetOrCreateMaterial("SpikeRed", new Color(0.82f, 0.22f, 0.18f), true, 0.4f);
        Material obstacleSteel = GetOrCreateMaterial("ObstacleSteel", new Color(0.12f, 0.12f, 0.15f), false, 0.3f);
        Material obstacleWarn = GetOrCreateMaterial("ObstacleWarn", new Color(1.00f, 0.48f, 0.12f), true, 0.6f);
        Material strutDark = GetOrCreateMaterial("StrutDark", new Color(0.05f, 0.05f, 0.07f), false, 0.15f);

        foreach (var cell in level.cells)
        {
            Vector3 worldPos = new Vector3(cell.coord.x * cellSpacing, 0f, cell.coord.y * cellSpacing);
            string tileName = $"{cell.type}Tile_{cell.coord.x}_{cell.coord.y}";

            GameObject tileObj = new GameObject(tileName);
            tileObj.transform.SetParent(boardRoot, false);
            tileObj.transform.localPosition = worldPos;

            Tile tileComp;
            switch (cell.type)
            {
                case TileType.Start:
                    tileComp = tileObj.AddComponent<StartTile>();
                    break;
                case TileType.Lava:
                    tileComp = tileObj.AddComponent<LavaTile>();
                    break;
                case TileType.End:
                    tileComp = tileObj.AddComponent<EndTile>();
                    break;
                case TileType.Obstacle:
                    tileComp = tileObj.AddComponent<ObstacleTile>();
                    break;
                default:
                    tileComp = tileObj.AddComponent<FreeTile>();
                    break;
            }

            tileComp.gridPosition = cell.coord;
            tileComp.tileType = cell.type;
            tileComp.hasObstacle = cell.hasObstacle || cell.type == TileType.Obstacle;

            // Optional custom prefab override
            GameObject prefabOverride = null;
            if (cell.type == TileType.Start) prefabOverride = customStartPrefab;
            else if (cell.type == TileType.Free && !tileComp.hasObstacle) prefabOverride = customFreePrefab;
            else if (cell.type == TileType.Lava) prefabOverride = customLavaPrefab;
            else if (cell.type == TileType.Obstacle || tileComp.hasObstacle) prefabOverride = customObstaclePrefab;
            else if (cell.type == TileType.End) prefabOverride = customEndPrefab;

            if (prefabOverride != null)
            {
                GameObject customVis = Object.Instantiate(prefabOverride, tileObj.transform);
                customVis.transform.localPosition = Vector3.zero;
                continue;
            }

            // Built-in polished 3D isometric industrial platform
            CreateBox(tileObj.transform, "BaseBlock", new Vector3(0f, -0.18f, 0f), new Vector3(1.82f, 0.36f, 1.82f), darkChassis);
            CreateBox(tileObj.transform, "TopDeck", new Vector3(0f, 0.02f, 0f), new Vector3(1.68f, 0.08f, 1.68f), tileTop);
            CreateBox(tileObj.transform, "UnderPillar", new Vector3(0f, -1.35f, 0f), new Vector3(0.42f, 2.0f, 0.42f), strutDark);

            if (cell.type == TileType.Start)
            {
                CreateBox(tileObj.transform, "StartPadRing", new Vector3(0f, 0.07f, 0f), new Vector3(1.25f, 0.04f, 1.25f), startGlow);
                CreateBox(tileObj.transform, "StartPadCenter", new Vector3(0f, 0.085f, 0f), new Vector3(0.85f, 0.04f, 0.85f), darkChassis);
            }
            else if (cell.type == TileType.Lava)
            {
                // Glowing red hazard strip + 3D spikes (matching screenshot!)
                CreateBox(tileObj.transform, "HazardBase", new Vector3(0f, 0.07f, 0f), new Vector3(1.52f, 0.08f, 1.52f), lavaGlow);
                for (int sx = -1; sx <= 1; sx++)
                {
                    for (int sz = -1; sz <= 1; sz++)
                    {
                        var spike = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        spike.name = $"Spike_{sx}_{sz}";
                        spike.transform.SetParent(tileObj.transform, false);
                        spike.transform.localPosition = new Vector3(sx * 0.44f, 0.22f, sz * 0.44f);
                        spike.transform.localScale = new Vector3(0.32f, 0.48f, 0.32f);
                        spike.GetComponent<Renderer>().sharedMaterial = spikeRed;
                        Object.DestroyImmediate(spike.GetComponent<Collider>());
                    }
                }
            }
            else if (cell.type == TileType.End)
            {
                // Glowing goal pad + industrial doorway frame + top gear (matching screenshot!)
                CreateBox(tileObj.transform, "GoalPad", new Vector3(0f, 0.07f, 0f), new Vector3(1.36f, 0.05f, 1.36f), endGlow);
                CreateBox(tileObj.transform, "GateLeftPost", new Vector3(0.45f, 0.85f, 0.62f), new Vector3(0.18f, 1.65f, 0.18f), darkChassis);
                CreateBox(tileObj.transform, "GateRightPost", new Vector3(0.45f, 0.85f, -0.62f), new Vector3(0.18f, 1.65f, 0.18f), darkChassis);
                CreateBox(tileObj.transform, "GateHeader", new Vector3(0.45f, 1.68f, 0f), new Vector3(0.22f, 0.24f, 1.48f), darkChassis);
                CreateBox(tileObj.transform, "GateNeonTrim", new Vector3(0.35f, 1.68f, 0f), new Vector3(0.06f, 0.08f, 1.24f), endGlow);

                // Decorative gear on top of the gate
                var gear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                gear.name = "GoalGear";
                gear.transform.SetParent(tileObj.transform, false);
                gear.transform.localPosition = new Vector3(0.45f, 2.02f, 0f);
                gear.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                gear.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
                gear.GetComponent<Renderer>().sharedMaterial = darkChassis;
                Object.DestroyImmediate(gear.GetComponent<Collider>());
            }

            if (tileComp.hasObstacle)
            {
                GameObject obsRoot = new GameObject("ObstacleBarrier");
                obsRoot.transform.SetParent(tileObj.transform, false);
                obsRoot.transform.localPosition = Vector3.zero;

                CreateBox(obsRoot.transform, "BarrierBlock", new Vector3(0f, 0.62f, 0f), new Vector3(1.38f, 1.15f, 1.38f), obstacleSteel);
                CreateBox(obsRoot.transform, "HazardStripeTop", new Vector3(0f, 0.95f, 0f), new Vector3(1.42f, 0.14f, 1.42f), obstacleWarn);
                CreateBox(obsRoot.transform, "HazardStripeBot", new Vector3(0f, 0.35f, 0f), new Vector3(1.42f, 0.14f, 1.42f), obstacleWarn);
                tileComp.obstacleVisual = obsRoot;
            }
        }
    }

    public static void StyleRobot(PlayerController player, GameObject customRobotPrefab = null)
    {
        if (player == null) return;

        // Hide default primitive capsule mesh on root so child silhouette robot shines
        var rootRenderer = player.GetComponent<MeshRenderer>();
        if (rootRenderer != null) rootRenderer.enabled = false;

        Transform existingVis = player.transform.Find("RobotVisualRoot");
        if (existingVis != null)
            Object.DestroyImmediate(existingVis.gameObject);

        GameObject visRoot = new GameObject("RobotVisualRoot");
        visRoot.transform.SetParent(player.transform, false);
        visRoot.transform.localPosition = new Vector3(0f, -0.45f, 0f);
        visRoot.transform.localRotation = Quaternion.identity;

        if (customRobotPrefab != null)
        {
            Object.Instantiate(customRobotPrefab, visRoot.transform);
            return;
        }

        // Blocky dark silhouette robot with two vertical glowing eyes (matching screenshot!)
        Material bodyMat = GetOrCreateMaterial("RobotSilhouette", new Color(0.05f, 0.05f, 0.07f), false, 0.25f);
        Material trimMat = GetOrCreateMaterial("RobotTrim", new Color(0.14f, 0.15f, 0.19f), false, 0.4f);
        Material eyeMat = GetOrCreateMaterial("RobotEyeGlow", new Color(0.75f, 0.92f, 1.00f), true, 0.9f);

        // Legs
        CreateBox(visRoot.transform, "LeftLeg", new Vector3(0f, 0.18f, 0.18f), new Vector3(0.26f, 0.36f, 0.22f), bodyMat);
        CreateBox(visRoot.transform, "RightLeg", new Vector3(0f, 0.18f, -0.18f), new Vector3(0.26f, 0.36f, 0.22f), bodyMat);
        // Torso & Head (square blocky robot from screenshot)
        CreateBox(visRoot.transform, "BodyHead", new Vector3(0f, 0.68f, 0f), new Vector3(0.68f, 0.74f, 0.72f), bodyMat);
        CreateBox(visRoot.transform, "TopCrown", new Vector3(0f, 1.08f, 0f), new Vector3(0.52f, 0.08f, 0.56f), trimMat);
        // Stubby Side Arms
        CreateBox(visRoot.transform, "LeftArm", new Vector3(0f, 0.58f, 0.46f), new Vector3(0.28f, 0.32f, 0.20f), bodyMat);
        CreateBox(visRoot.transform, "RightArm", new Vector3(0f, 0.58f, -0.46f), new Vector3(0.28f, 0.32f, 0.20f), bodyMat);
        // Glowing vertical eyes on front (+Z in local space since robot rotates to 90 Y = +X world)
        CreateBox(visRoot.transform, "LeftEye", new Vector3(0.14f, 0.74f, 0.35f), new Vector3(0.10f, 0.24f, 0.06f), eyeMat);
        CreateBox(visRoot.transform, "RightEye", new Vector3(-0.14f, 0.74f, 0.35f), new Vector3(0.10f, 0.24f, 0.06f), eyeMat);
        // Also put eyes visible toward camera side (+X/-Z isometric angle) so they're always visible from the isometric camera!
        CreateBox(visRoot.transform, "FrontLeftEye", new Vector3(0.35f, 0.74f, 0.14f), new Vector3(0.06f, 0.24f, 0.10f), eyeMat);
        CreateBox(visRoot.transform, "FrontRightEye", new Vector3(0.35f, 0.74f, -0.14f), new Vector3(0.06f, 0.24f, 0.10f), eyeMat);
    }

    public static void FrameCameraForLevel(Camera cam, LevelDefinition level, float cellSpacing)
    {
        if (cam == null || level == null || level.cells.Count == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var c in level.cells)
        {
            float wx = c.coord.x * cellSpacing;
            float wz = c.coord.y * cellSpacing;
            if (wx < minX) minX = wx;
            if (wx > maxX) maxX = wx;
            if (wz < minZ) minZ = wz;
            if (wz > maxZ) maxZ = wz;
        }

        Vector3 center = new Vector3((minX + maxX) * 0.5f, 0.35f, (minZ + maxZ) * 0.5f);
        float spanX = Mathf.Max(6f, (maxX - minX) + 4f);
        float spanZ = Mathf.Max(3f, (maxZ - minZ) + 3f);

        // Top half of screen is the 3D isometric scene (Y: 0.48 to 1.0)
        cam.rect = new Rect(0f, 0.48f, 1f, 0.52f);
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Clamp(Mathf.Max(spanX * 0.28f, spanZ * 0.55f), 2.8f, 6.4f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.22f, 0.05f, 0.07f, 1f); // Deep industrial crimson-black

        // True isometric camera angle looking across the track
        Quaternion isoRot = Quaternion.Euler(24f, -32f, 0f);
        cam.transform.rotation = isoRot;
        cam.transform.position = center - (isoRot * Vector3.forward) * 18f;

        EnsureFactoryBackdrop(cam, center, isoRot);
    }

    private static void EnsureFactoryBackdrop(Camera cam, Vector3 boardCenter, Quaternion camRot)
    {
        Transform existing = cam.transform.Find("FactoryBackdropQuad");
        if (existing == null)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FactoryBackdropQuad";
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.transform.SetParent(cam.transform, false);
            existing = quad.transform;
        }

        float h = cam.orthographicSize * 2.15f;
        float w = h * 3.4f;
        existing.localPosition = new Vector3(0f, 0f, 32f);
        existing.localRotation = Quaternion.identity;
        existing.localScale = new Vector3(w, h, 1f);

        Texture2D tex = Resources.Load<Texture2D>("LoopLab/FactoryBackdrop");
#if UNITY_EDITOR
        if (tex == null)
            tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/FactoryBackdrop.png");
#endif
        Shader unlit = Shader.Find("Unlit/Texture");
        if (unlit == null) unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = Shader.Find("Sprites/Default");

        var mat = new Material(unlit) { name = "Mat_FactoryBackdrop" };
        if (tex != null)
        {
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        }
        else
        {
            mat.color = new Color(0.25f, 0.06f, 0.08f);
        }
        existing.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
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
