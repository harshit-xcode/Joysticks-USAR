#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Editor builder and scene setup utility for the Isometric 3D + Node-Graph Puzzle Game.
public static class CommandPanelRepair
{
    [MenuItem("Tools/Joysticks/Setup GameJamLevel 1 (3D Isometric + Node Lab)")]
    public static void RepairFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before running Scene Setup.");
            return;
        }
        SetupActiveSceneForLevel(0);
    }

    [MenuItem("Tools/Joysticks/Preview Level 1 (Closed Loop - Walk)")]
    public static void PreviewL1() => SetupActiveSceneForLevel(0);

    [MenuItem("Tools/Joysticks/Preview Level 2 (Phase Leap - True Jump)")]
    public static void PreviewL2() => SetupActiveSceneForLevel(1);

    [MenuItem("Tools/Joysticks/Preview Level 3 (Wall Clamp - Obstacle Asymmetry)")]
    public static void PreviewL3() => SetupActiveSceneForLevel(2);

    [MenuItem("Tools/Joysticks/Preview Level 4 (Phase Weave - Act I Finale)")]
    public static void PreviewL4() => SetupActiveSceneForLevel(3);

    [MenuItem("Tools/Joysticks/Preview Level 5 (Phase Pulse - EMP + Dynamic Gates)")]
    public static void PreviewL5() => SetupActiveSceneForLevel(4);

    [MenuItem("Tools/Joysticks/Preview Level 6 (Chrono Overdrive - Phase Weave)")]
    public static void PreviewL6() => SetupActiveSceneForLevel(5);

    [MenuItem("Tools/Joysticks/Preview Level 7 (Dynamic Matrix - Grand Finale)")]
    public static void PreviewL7() => SetupActiveSceneForLevel(6);

    [MenuItem("Tools/Joysticks/Preview Level 8 (Rotation Run)")]
    public static void PreviewL8() => SetupActiveSceneForLevel(7);

    [MenuItem("Tools/Joysticks/Preview Level 9 (Matrix Weave)")]
    public static void PreviewL9() => SetupActiveSceneForLevel(8);

    [MenuItem("Tools/Joysticks/Test Run Level 9 Solution (Play Mode)")]
    public static void TestRunL9() => TestWireAndRun(8, new[]
    {
        BlockType.Walk, BlockType.Wait, BlockType.Walk, BlockType.MoveLeft,
        BlockType.Wait, BlockType.Walk, BlockType.Lava, BlockType.MoveRight
    }, true);

    [MenuItem("Tools/Joysticks/Test Wire Level 6 Solution Only (Play Mode)")]
    public static void TestWireL6Only() => TestWireAndRun(5, new[] { BlockType.Lava, BlockType.MoveLeft, BlockType.Wait, BlockType.Walk, BlockType.MoveRight }, false);

    [MenuItem("Tools/Joysticks/Test Run Level 5 Solution (Play Mode)")]
    public static void TestRunL5() => TestWireAndRun(4, new[] { BlockType.Wait, BlockType.Lava, BlockType.Walk }, true);

    [MenuItem("Tools/Joysticks/Test Run Level 6 Solution (Play Mode)")]
    public static void TestRunL6() => TestWireAndRun(5, new[] { BlockType.Lava, BlockType.MoveLeft, BlockType.Wait, BlockType.Walk, BlockType.MoveRight }, true);

    [MenuItem("Tools/Joysticks/Test Run Level 7 Solution (Play Mode)")]
    public static void TestRunL7() => TestWireAndRun(6, new[] { BlockType.Lava, BlockType.MoveLeft, BlockType.Lava, BlockType.MoveRight }, true);

    [MenuItem("Tools/Joysticks/Test Run Level 8 Solution (Play Mode)")]
    public static void TestRunL8() => TestWireAndRun(7, new[] { BlockType.Walk, BlockType.Wait, BlockType.Lava, BlockType.MoveLeft }, true);



    private static void TestWireAndRun(int levelIdx, BlockType[] order, bool runImmediately)
    {
        if (LevelManager.Instance == null || UIManager.Instance == null) return;
        LevelManager.Instance.LoadCampaignLevel(levelIdx, true);
        var ui = UIManager.Instance;
        NodeCardUI prev = ui.startNodeCard;
        foreach (var bt in order)
        {
            foreach (var pc in ui.paletteCards)
            {
                if (pc != null && pc.gameObject.activeSelf && pc.blockType == bt)
                {
                    ui.QuickPlaceFromPalette(pc);
                    break;
                }
            }
            NodeCardUI placed = ui.graphNodes[ui.graphNodes.Count - 1];
            prev.connectedNext = placed;
            placed.connectedPrev = prev;
            prev = placed;
        }
        prev.connectedNext = ui.endNodeCard;
        ui.endNodeCard.connectedPrev = prev;
        ui.RefreshWires();
        if (runImmediately)
            ui.CollectBlocksAndRun();
    }

    public static void SetupActiveSceneForLevel(int levelIndex)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before running Scene Setup.");
            return;
        }
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                "Rebuild and Save Active Scene?",
                "This rebuilds the board and configures the UI, then saves the active scene. Scene-authored changes may be replaced.",
                "Rebuild Scene", "Cancel")) return;

        AssetDatabase.Refresh();
        EnsureTextureImportSettings();

        var scene = EditorSceneManager.GetActiveScene();

        // 1. Ensure Managers exist & are wired
        var tileManager = Object.FindFirstObjectByType<TileManager>();
        if (tileManager == null)
            tileManager = new GameObject("TileManager").AddComponent<TileManager>();

        var levelManager = Object.FindFirstObjectByType<LevelManager>();
        if (levelManager == null)
            levelManager = new GameObject("LevelManager").AddComponent<LevelManager>();

        var codeExecutor = Object.FindFirstObjectByType<CodeExecutor>();
        if (codeExecutor == null)
            codeExecutor = new GameObject("CodeExecuter").AddComponent<CodeExecutor>();

        var uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
            uiManager = new GameObject("UIManager").AddComponent<UIManager>();

        var audioManager = Object.FindFirstObjectByType<AudioManager>();
        if (audioManager == null)
            audioManager = new GameObject("AudioManager").AddComponent<AudioManager>();

        WireAudioAssets(audioManager);
        WireUiSpritesAndPrefabs(uiManager);

        // 2. Ensure Board Root
        GameObject boardObj = GameObject.Find("GameObject");
        if (boardObj == null) boardObj = GameObject.Find("IsometricBoard");
        if (boardObj == null) boardObj = new GameObject("GameObject");

        // 3. Ensure Robot Player
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            var robotGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            robotGo.name = "Robot";
            player = robotGo.AddComponent<PlayerController>();
            robotGo.AddComponent<PlayerRespawn>();
        }
        if (player.GetComponent<PlayerRespawn>() == null)
            player.gameObject.AddComponent<PlayerRespawn>();

        // 4. Wire references across managers
        levelManager.player = player;
        levelManager.uiManager = uiManager;
        levelManager.boardRoot = boardObj.transform;
        levelManager.cellSpacing = 2.0f;
        levelManager.campaignLevels = LevelCampaignCatalog.CreateDefault7Levels();
        levelManager.currentLevelIndex = Mathf.Clamp(levelIndex, 0, levelManager.campaignLevels.Count - 1);

        codeExecutor.player = player;
        uiManager.codeExecutor = codeExecutor;
        uiManager.player = player;

        // 5. Ensure EventSystem
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // 6. Ensure Directional Light
        var light = Object.FindFirstObjectByType<Light>();
        if (light == null)
        {
            light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.transform.rotation = Quaternion.Euler(48f, -38f, 0f);
        light.intensity = 1.25f;
        light.color = new Color(1.0f, 0.92f, 0.88f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.34f, 0.36f);

        // 7. Build 3D Isometric Board + Style Robot + Frame Camera + Build Split-Screen UI
        LevelDefinition targetLevel = levelManager.campaignLevels[levelManager.currentLevelIndex];
        IsometricBoardBuilder.BuildLevelOnBoard(
            boardObj.transform,
            targetLevel,
            levelManager.cellSpacing,
            levelManager.customStartTilePrefab,
            levelManager.customFreeTilePrefab,
            levelManager.customLavaTilePrefab,
            levelManager.customObstacleTilePrefab,
            levelManager.customEndTilePrefab);

        tileManager.cellSpacing = levelManager.cellSpacing;
        tileManager.RegisterAllTiles();

        IsometricBoardBuilder.StyleRobot(player, levelManager.customRobotVisualPrefab);
        player.PlaceOnStartTile();

        if (levelManager.customBackdropTexture == null)
        {
            levelManager.customBackdropTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/UI/FactoryBackdrop.png");
        }

        if (Camera.main != null)
        {
            IsometricBoardBuilder.FrameCameraForLevel(Camera.main, targetLevel, levelManager.cellSpacing, levelManager.customBackdropTexture);
        }

        uiManager.EnsureSplitScreenNodeLabUI();
        uiManager.ConfigureForLevel(targetLevel, levelManager.currentLevelIndex, levelManager.campaignLevels.Count);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Scene '{scene.name}' configured and saved for {targetLevel.title}!");
    }

    private static void EnsureTextureImportSettings()
    {
        string[] paths =
        {
            "Assets/Textures/UI/NodeCardFrame.png",
            "Assets/Textures/UI/LedPill.png",
            "Assets/Textures/UI/PortSquare.png",
            "Assets/Textures/UI/PortTriangle.png",
            "Assets/Textures/UI/Icon_Walk.png",
            "Assets/Textures/UI/Icon_Left.png",
            "Assets/Textures/UI/Icon_Right.png",
            "Assets/Textures/UI/Icon_Jump.png",
            "Assets/Textures/UI/Icon_Force.png",
            "Assets/Textures/UI/Icon_Start.png",
            "Assets/Textures/UI/Icon_End.png",
            "Assets/Textures/UI/Icon_Wait.png",
            "Assets/Textures/UI/Icon_PlayBtn.png",
            "Assets/Textures/UI/Icon_StopBtn.png",
            "Assets/Textures/UI/Icon_ResetBtn.png",
            "Assets/Textures/UI/LabGridTile.png",
            "Assets/Resources/LoopLab/NodeCardFrame.png",
            "Assets/Resources/LoopLab/LedPill.png",
            "Assets/Resources/LoopLab/PortSquare.png",
            "Assets/Resources/LoopLab/PortTriangle.png",
            "Assets/Resources/LoopLab/Icon_Walk.png",
            "Assets/Resources/LoopLab/Icon_Left.png",
            "Assets/Resources/LoopLab/Icon_Right.png",
            "Assets/Resources/LoopLab/Icon_Jump.png",
            "Assets/Resources/LoopLab/Icon_Force.png",
            "Assets/Resources/LoopLab/Icon_Start.png",
            "Assets/Resources/LoopLab/Icon_End.png",
            "Assets/Resources/LoopLab/Icon_Wait.png",
            "Assets/Resources/LoopLab/Icon_PlayBtn.png",
            "Assets/Resources/LoopLab/Icon_StopBtn.png",
            "Assets/Resources/LoopLab/Icon_ResetBtn.png",
            "Assets/Resources/LoopLab/LabGridTile.png"
        };

        foreach (string p in paths)
        {
            var importer = AssetImporter.GetAtPath(p) as TextureImporter;
            if (importer == null) continue;
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }
            if (p.Contains("LabGridTile") && importer.wrapMode != TextureWrapMode.Repeat)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
        }
    }

    private static void WireAudioAssets(AudioManager audio)
    {
        if (audio == null) return;
        audio.backgroundMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM_FactoryLoop.wav");
        audio.walkSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Walk.wav");
        audio.jumpSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Jump.wav");
        audio.forceSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Force.wav");
        audio.bumpSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Bump.wav");
        audio.lavaFailSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_LavaFail.wav");
        audio.nodePlaceSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_NodePlace.wav");
        audio.nodeConnectSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_NodeConnect.wav");
        audio.winSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX_Win.wav");
        EditorUtility.SetDirty(audio);
    }

    private static void WireUiSpritesAndPrefabs(UIManager ui)
    {
        if (ui == null) return;
        ui.nodeCardFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/NodeCardFrame.png");
        ui.ledPillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/LedPill.png");
        ui.portSquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/PortSquare.png");
        ui.portTriangleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/PortTriangle.png");
        ui.labGridTileSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/LabGridTile.png");
        ui.playBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Icon_PlayBtn.png");
        ui.stopBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Icon_StopBtn.png");
        ui.resetBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/Icon_ResetBtn.png");

        if (ui.startBlockPrefab == null)
            ui.startBlockPrefab = AssetDatabase.LoadAssetAtPath<StartBlock>("Assets/Prefabs/StartBlockPrefab.prefab");
        if (ui.walkBlockPrefab == null)
            ui.walkBlockPrefab = AssetDatabase.LoadAssetAtPath<WalkBlock>("Assets/Prefabs/WalkBlockPrefab.prefab");
        if (ui.lavaBlockPrefab == null)
            ui.lavaBlockPrefab = AssetDatabase.LoadAssetAtPath<LavaBlock>("Assets/Prefabs/LavaBlockPrefab.prefab");
        if (ui.stopBlockPrefab == null)
            ui.stopBlockPrefab = AssetDatabase.LoadAssetAtPath<StopBlock>("Assets/Prefabs/StopBlockPrefab.prefab");

        EditorUtility.SetDirty(ui);
    }
}
#endif
