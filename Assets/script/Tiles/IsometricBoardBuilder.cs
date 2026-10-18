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
                case TileType.PhaseSpikeUp:
                case TileType.PhaseSpikeDown:
                    var spikeTile = tileObj.AddComponent<PhaseSpikeTile>();
                    spikeTile.startsExtended = (cell.type == TileType.PhaseSpikeUp);
                    tileComp = spikeTile;
                    break;
                case TileType.PhaseGateClosed:
                case TileType.PhaseGateOpen:
                    var gateTile = tileObj.AddComponent<PhaseGateTile>();
                    gateTile.startsClosed = (cell.type == TileType.PhaseGateClosed);
                    tileComp = gateTile;
                    break;
                default:
                    tileComp = tileObj.AddComponent<FreeTile>();
                    break;
            }

            tileComp.gridPosition = cell.coord;
            tileComp.tileType = cell.type;
            tileComp.hasObstacle = cell.hasObstacle || cell.type == TileType.Obstacle || cell.type == TileType.PhaseGateClosed;

            // Optional custom prefab override
            GameObject prefabOverride = null;
            if (cell.type == TileType.Start) prefabOverride = customStartPrefab;
            else if (cell.type == TileType.Free && !tileComp.hasObstacle) prefabOverride = customFreePrefab;
            else if (cell.type == TileType.Lava) prefabOverride = customLavaPrefab;
            else if (cell.type == TileType.Obstacle) prefabOverride = customObstaclePrefab;
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
            else if (cell.type == TileType.PhaseSpikeUp || cell.type == TileType.PhaseSpikeDown)
            {
                var phaseSpike = tileComp as PhaseSpikeTile;
                Material rimInitMat = GetOrCreateMaterial("PhaseSpikeRimInit", new Color(1.00f, 0.20f, 0.28f), true, 0.7f);
                Material pistonMat = GetOrCreateMaterial("PhaseSpikePiston", new Color(1.00f, 0.25f, 0.32f), true, 0.6f);

                // Recessed dark mechanical grate housing
                CreateBox(tileObj.transform, "RecessWell", new Vector3(0f, 0.055f, 0f), new Vector3(1.36f, 0.04f, 1.36f), strutDark);
                for (int g = -1; g <= 1; g++)
                {
                    CreateBox(tileObj.transform, $"GrateBar_{g}", new Vector3(g * 0.44f, 0.07f, 0f), new Vector3(0.10f, 0.03f, 1.32f), darkChassis);
                }

                // 4 Neon status rim bars around the housing
                var r1 = CreateBox(tileObj.transform, "StatusRimN", new Vector3(0f, 0.08f, 0.72f), new Vector3(1.52f, 0.05f, 0.08f), rimInitMat).GetComponent<Renderer>();
                var r2 = CreateBox(tileObj.transform, "StatusRimS", new Vector3(0f, 0.08f, -0.72f), new Vector3(1.52f, 0.05f, 0.08f), rimInitMat).GetComponent<Renderer>();
                var r3 = CreateBox(tileObj.transform, "StatusRimE", new Vector3(0.72f, 0.08f, 0f), new Vector3(0.08f, 0.05f, 1.38f), rimInitMat).GetComponent<Renderer>();
                var r4 = CreateBox(tileObj.transform, "StatusRimW", new Vector3(-0.72f, 0.08f, 0f), new Vector3(0.08f, 0.05f, 1.38f), rimInitMat).GetComponent<Renderer>();

                // Central rotating plasma turbine rotor
                GameObject coreRoot = new GameObject("RotatingTurbineCore");
                coreRoot.transform.SetParent(tileObj.transform, false);
                coreRoot.transform.localPosition = new Vector3(0f, 0.075f, 0f);
                var bladeA = CreateBox(coreRoot.transform, "RotorBladeA", Vector3.zero, new Vector3(0.76f, 0.03f, 0.14f), rimInitMat).GetComponent<Renderer>();
                var bladeB = CreateBox(coreRoot.transform, "RotorBladeB", Vector3.zero, new Vector3(0.14f, 0.03f, 0.76f), rimInitMat).GetComponent<Renderer>();

                // Movable Hydraulic Spike Rack
                GameObject rackRoot = new GameObject("HydraulicSpikeRack");
                rackRoot.transform.SetParent(tileObj.transform, false);
                rackRoot.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                for (int sx = -1; sx <= 1; sx++)
                {
                    for (int sz = -1; sz <= 1; sz++)
                    {
                        var spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        spike.name = $"PhasePiston_{sx}_{sz}";
                        spike.transform.SetParent(rackRoot.transform, false);
                        spike.transform.localPosition = new Vector3(sx * 0.42f, 0f, sz * 0.42f);
                        spike.transform.localScale = new Vector3(0.24f, 0.22f, 0.24f);
                        spike.GetComponent<Renderer>().sharedMaterial = pistonMat;
                        Object.DestroyImmediate(spike.GetComponent<Collider>());

                        var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        tip.name = $"PistonTip_{sx}_{sz}";
                        tip.transform.SetParent(rackRoot.transform, false);
                        tip.transform.localPosition = new Vector3(sx * 0.42f, 0.20f, sz * 0.42f);
                        tip.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
                        tip.GetComponent<Renderer>().sharedMaterial = rimInitMat;
                        Object.DestroyImmediate(tip.GetComponent<Collider>());
                    }
                }

                if (phaseSpike != null)
                {
                    phaseSpike.spikeRackTransform = rackRoot.transform;
                    phaseSpike.rotatingCoreTransform = coreRoot.transform;
                    phaseSpike.statusRimRenderers = new List<Renderer> { r1, r2, r3, r4, bladeA, bladeB };
                    phaseSpike.SyncVisualImmediate();
                }
            }
            else if (cell.type == TileType.PhaseGateClosed || cell.type == TileType.PhaseGateOpen)
            {
                var phaseGate = tileComp as PhaseGateTile;
                Material laserBeamMat = GetOrCreateMaterial("PhaseLaserBeam", new Color(1.00f, 0.16f, 0.34f), true, 0.85f);
                Material laserCoreMat = GetOrCreateMaterial("PhaseLaserCore", new Color(1.00f, 0.45f, 0.62f), true, 0.90f);

                // Floor emitter emitter strip
                CreateBox(tileObj.transform, "GateFloorTrack", new Vector3(0f, 0.065f, 0f), new Vector3(0.32f, 0.04f, 1.56f), strutDark);

                // Left & Right Industrial Laser Pylons
                CreateBox(tileObj.transform, "PylonBaseL", new Vector3(0f, 0.16f, 0.68f), new Vector3(0.42f, 0.22f, 0.32f), obstacleSteel);
                CreateBox(tileObj.transform, "PylonTowerL", new Vector3(0f, 0.62f, 0.68f), new Vector3(0.26f, 0.96f, 0.22f), darkChassis);
                CreateBox(tileObj.transform, "PylonBaseR", new Vector3(0f, 0.16f, -0.68f), new Vector3(0.42f, 0.22f, 0.32f), obstacleSteel);
                CreateBox(tileObj.transform, "PylonTowerR", new Vector3(0f, 0.62f, -0.68f), new Vector3(0.26f, 0.96f, 0.22f), darkChassis);

                // Beacon status strips on towers
                var bL = CreateBox(tileObj.transform, "PylonBeaconL", new Vector3(0f, 0.64f, 0.68f), new Vector3(0.30f, 0.72f, 0.08f), laserBeamMat).GetComponent<Renderer>();
                var bR = CreateBox(tileObj.transform, "PylonBeaconR", new Vector3(0f, 0.64f, -0.68f), new Vector3(0.30f, 0.72f, 0.08f), laserBeamMat).GetComponent<Renderer>();

                // Rotating top emitter orbs
                var orbL = CreateBox(tileObj.transform, "LeftEmitterOrb", new Vector3(0f, 1.22f, 0.68f), new Vector3(0.28f, 0.28f, 0.28f), laserCoreMat);
                orbL.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
                var orbR = CreateBox(tileObj.transform, "RightEmitterOrb", new Vector3(0f, 1.22f, -0.68f), new Vector3(0.28f, 0.28f, 0.28f), laserCoreMat);
                orbR.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);

                // Horizontal Laser Wall Beams
                GameObject beamsRoot = new GameObject("LaserWallBeams");
                beamsRoot.transform.SetParent(tileObj.transform, false);
                beamsRoot.transform.localPosition = Vector3.zero;
                CreateBox(beamsRoot.transform, "LaserBarBottom", new Vector3(0f, 0.32f, 0f), new Vector3(0.12f, 0.08f, 1.28f), laserBeamMat);
                CreateBox(beamsRoot.transform, "LaserBarMiddle", new Vector3(0f, 0.62f, 0f), new Vector3(0.14f, 0.09f, 1.28f), laserCoreMat);
                CreateBox(beamsRoot.transform, "LaserBarTop", new Vector3(0f, 0.92f, 0f), new Vector3(0.12f, 0.08f, 1.28f), laserBeamMat);

                tileComp.obstacleVisual = beamsRoot;
                if (phaseGate != null)
                {
                    phaseGate.laserBeamsRoot = beamsRoot.transform;
                    phaseGate.leftOrbTransform = orbL.transform;
                    phaseGate.rightOrbTransform = orbR.transform;
                    phaseGate.beaconRenderers = new List<Renderer> { bL, bR, orbL.GetComponent<Renderer>(), orbR.GetComponent<Renderer>() };
                    phaseGate.SyncVisualImmediate();
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

            if (tileComp.hasObstacle && cell.type != TileType.PhaseGateClosed && cell.type != TileType.PhaseGateOpen)
            {
                GameObject obsRoot = new GameObject("ObstacleBarrier");
                obsRoot.transform.SetParent(tileObj.transform, false);
                obsRoot.transform.localPosition = Vector3.zero;

                CreateBox(obsRoot.transform, "BarrierBlock", new Vector3(0f, 0.62f, 0f), new Vector3(1.38f, 1.15f, 1.38f), obstacleSteel);
                CreateBox(obsRoot.transform, "HazardStripeTop", new Vector3(0f, 0.95f, 0f), new Vector3(1.42f, 0.14f, 1.42f), obstacleWarn);
                CreateBox(obsRoot.transform, "HazardStripeBot", new Vector3(0f, 0.35f, 0f), new Vector3(1.42f, 0.14f, 1.42f), obstacleWarn);
                tileComp.obstacleVisual = obsRoot;
            }

            AddRoleAccent(tileObj.transform, cell, showWalkArrow: level.levelIndex != 8);
        }
    }

    // Visual-only role highlight: colored deck rim plus an optional floor symbol (no gameplay effect).
    private static void AddRoleAccent(Transform tile, TileCellData cell, bool showWalkArrow)
    {
        if (cell.accent == TileAccent.None) return;

        Color rimColor;
        switch (cell.accent)
        {
            case TileAccent.Gate: rimColor = new Color(1.00f, 0.36f, 0.04f); break;
            case TileAccent.Rotate: rimColor = new Color(0.72f, 0.28f, 1.00f); break;
            case TileAccent.Jump: rimColor = new Color(1.00f, 0.84f, 0.12f); break;
            default: rimColor = new Color(0.15f, 0.85f, 1.00f); break;
        }
        Material rim = GetOrCreateMaterial("AccentRim", rimColor, true, 0.7f);

        GameObject root = new GameObject("RoleAccent");
        root.transform.SetParent(tile, false);
        CreateBox(root.transform, "RimN", new Vector3(0f, 0.075f, 0.79f), new Vector3(1.66f, 0.04f, 0.07f), rim);
        CreateBox(root.transform, "RimS", new Vector3(0f, 0.075f, -0.79f), new Vector3(1.66f, 0.04f, 0.07f), rim);
        CreateBox(root.transform, "RimE", new Vector3(0.79f, 0.075f, 0f), new Vector3(0.07f, 0.04f, 1.52f), rim);
        CreateBox(root.transform, "RimW", new Vector3(-0.79f, 0.075f, 0f), new Vector3(0.07f, 0.04f, 1.52f), rim);

        switch (cell.accent)
        {
            case TileAccent.Walk:
            {
                // L8 keeps its cyan walk-tile rims without direction arrows.
                if (!showWalkArrow) break;

                // Forward (+X) arrow, matching the WALK command direction
                CreateBox(root.transform, "ArrowShaft", new Vector3(-0.12f, 0.08f, 0f), new Vector3(0.56f, 0.03f, 0.12f), rim);
                var h1 = CreateBox(root.transform, "ArrowHeadA", new Vector3(0.20f, 0.08f, 0.10f), new Vector3(0.34f, 0.03f, 0.11f), rim);
                h1.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                var h2 = CreateBox(root.transform, "ArrowHeadB", new Vector3(0.20f, 0.08f, -0.10f), new Vector3(0.34f, 0.03f, 0.11f), rim);
                h2.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);
                break;
            }
            case TileAccent.Rotate:
            {
                // Clockwise (seen from above) 270-degree arc with an arrowhead at its end
                const float radius = 0.42f;
                const int segs = 10;
                float startDeg = 135f, endDeg = -135f;
                for (int i = 0; i < segs; i++)
                {
                    float midDeg = Mathf.Lerp(startDeg, endDeg, (i + 0.5f) / segs);
                    float rad = midDeg * Mathf.Deg2Rad;
                    var seg = CreateBox(root.transform, $"ArcSeg_{i}", new Vector3(Mathf.Cos(rad) * radius, 0.08f, Mathf.Sin(rad) * radius),
                        new Vector3(0.11f, 0.03f, 2f * Mathf.PI * radius * 0.75f / segs + 0.02f), rim);
                    seg.transform.localRotation = Quaternion.Euler(0f, -midDeg, 0f);
                }
                float endRad = endDeg * Mathf.Deg2Rad;
                Vector3 tip = new Vector3(Mathf.Cos(endRad) * radius, 0.08f, Mathf.Sin(endRad) * radius);
                Vector3 travel = new Vector3(Mathf.Sin(endRad), 0f, -Mathf.Cos(endRad)); // tangent for decreasing angle
                float travelYaw = Mathf.Atan2(travel.x, travel.z) * Mathf.Rad2Deg;
                var hA = CreateBox(root.transform, "ArcHeadA", tip - Quaternion.Euler(0f, travelYaw + 35f, 0f) * Vector3.forward * 0.12f, new Vector3(0.10f, 0.03f, 0.28f), rim);
                hA.transform.localRotation = Quaternion.Euler(0f, travelYaw + 35f, 0f);
                var hB = CreateBox(root.transform, "ArcHeadB", tip - Quaternion.Euler(0f, travelYaw - 35f, 0f) * Vector3.forward * 0.12f, new Vector3(0.10f, 0.03f, 0.28f), rim);
                hB.transform.localRotation = Quaternion.Euler(0f, travelYaw - 35f, 0f);
                break;
            }
            case TileAccent.Jump:
            {
                // Flat jump arc (take-off -> landing along +X) with a landing dot, drawn on the deck
                const int segs = 8;
                for (int i = 0; i < segs; i++)
                {
                    Vector3 a = JumpArcPoint(i / (float)segs);
                    Vector3 b = JumpArcPoint((i + 1) / (float)segs);
                    Vector3 d = b - a;
                    var seg = CreateBox(root.transform, $"JumpArc_{i}", (a + b) * 0.5f, new Vector3(0.10f, 0.03f, d.magnitude + 0.03f), rim);
                    seg.transform.localRotation = Quaternion.Euler(0f, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0f);
                }
                CreateBox(root.transform, "JumpLanding", JumpArcPoint(1f) + new Vector3(0.02f, 0f, -0.08f), new Vector3(0.16f, 0.03f, 0.16f), rim);
                break;
            }
            case TileAccent.End:
            {
                Transform pad = tile.Find("GoalPad");
                if (pad != null) pad.GetComponent<Renderer>().sharedMaterial = rim;
                break;
            }
        }
    }

    // Arc in the deck plane: x runs take-off -> landing, z is the "height" of the jump as seen from above.
    private static Vector3 JumpArcPoint(float t)
    {
        float x = Mathf.Lerp(-0.48f, 0.48f, t);
        float u = (t - 0.5f) * 2f;
        return new Vector3(x, 0.08f, -0.22f + 0.48f * (1f - u * u));
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

    public static void FrameCameraForLevel(Camera cam, LevelDefinition level, float cellSpacing, Texture2D customBackdropTex = null)
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
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.22f, 0.05f, 0.07f, 1f); // Deep industrial crimson-black

        // Frame close near the user's favorite P3 sweet spot (~3.4f)!
        // Previously: Mathf.Clamp(Mathf.Max(spanX * 0.28f, spanZ * 0.55f), 2.8f, 6.4f) pushed levels to 6.4f (way too far)!
        float defaultSweetSpot = Mathf.Clamp(Mathf.Max(spanX * 0.16f, spanZ * 0.42f) + 1.15f, 3.2f, 4.0f);

        var rig = cam.GetComponent<GameCameraController>();
        if (rig == null) rig = cam.gameObject.AddComponent<GameCameraController>();
        rig.targetCamera = cam;
        // Full platform/robot envelope: deck edges, pillar bottoms and the tallest exit ornament.
        var levelBounds = new Bounds(new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f),
            new Vector3(maxX - minX + 2f, 5f, maxZ - minZ + 2f));
        rig.SetLevelBounds(levelBounds, level.fitCameraToBoard);
        rig.SetBoardCenter(center, defaultSweetSpot);

        EnsureFactoryBackdrop(cam, center, cam.transform.rotation, customBackdropTex);
    }

    private static void EnsureFactoryBackdrop(Camera cam, Vector3 boardCenter, Quaternion camRot, Texture2D customBackdropTex = null)
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
        float w = h * Mathf.Max(3.4f, cam.aspect * 1.05f);
        var rig = cam.GetComponent<GameCameraController>();
        float backdropDistance = rig != null ? rig.BackdropDistance : 32f;
        existing.localPosition = new Vector3(0f, 0f, backdropDistance);
        existing.localRotation = Quaternion.identity;
        existing.localScale = new Vector3(w, h, 1f);

        Renderer rend = existing.GetComponent<Renderer>();

        // If user dropped a custom material/texture directly on FactoryBackdropQuad in the Inspector, keep it!
        if (customBackdropTex == null && rend != null && rend.sharedMaterial != null && rend.sharedMaterial.mainTexture != null && rend.sharedMaterial.name != "Mat_FactoryBackdrop")
        {
            return;
        }

        Texture2D tex = customBackdropTex;
        if (tex == null)
            tex = Resources.Load<Texture2D>("LoopLab/FactoryBackdrop");
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
        rend.sharedMaterial = mat;
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
