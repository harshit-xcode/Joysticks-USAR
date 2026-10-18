#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Optional editor checks for P9. Batch validation runs on a project copy, never on the user's open scene.
[InitializeOnLoad]
public static class MatrixWeaveValidation
{
    const string StateKey = "MatrixWeave.Validation.State";
    static double deadline;
    static int steps;
    static int failures;

    static MatrixWeaveValidation()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Joysticks/Validate Matrix Weave Definition")]
    public static void ValidateDefinition()
    {
        var campaign = LevelCampaignCatalog.CreateCampaignLevels();
        Check(campaign.Count == 9 && campaign.Select(l => l.title).Distinct().Count() == 9,
            "Nine distinct campaign levels");
        Check(campaign[6].title == "PROTOCOL 07 // DYNAMIC MATRIX" &&
              campaign[7].title == "PROTOCOL 08 // ROTATION RUN", "P7/P8 preserved");
        var level = campaign[8];
        Check(level.cells.Count == 32 && level.cells.Select(c => c.coord).Distinct().Count() == 32,
            "Two complete lanes with no duplicate cells");
        Check(level.cells.Single(c => c.type == TileType.Start).coord == new Vector2Int(0, 0), "Start lower 0");
        Check(level.cells.Single(c => c.type == TileType.End).coord == new Vector2Int(15, 0), "Exit lower 15");
        Check(level.unlockedNodes.Sum(n => n.count) == 8 && level.unlockedNodes.Count == 5,
            "Eight modules from five existing action types");
        foreach (var node in level.unlockedNodes)
            Check(node.nodeType == BlockType.Walk || node.nodeType == BlockType.Wait ||
                  node.nodeType == BlockType.Lava || node.nodeType == BlockType.MoveLeft ||
                  node.nodeType == BlockType.MoveRight, "Existing action only");
        for (int sector = 0; sector < 3; sector++)
        {
            int x = sector * 5;
            Check(level.cells.Single(c => c.coord == new Vector2Int(x + 2, 0)).type == TileType.PhaseGateClosed,
                "Closed gate in sector " + sector);
            Check(level.cells.Single(c => c.coord == new Vector2Int(x + 3, 0)).type == TileType.Obstacle,
                "Lower wall in sector " + sector);
            Check(level.cells.Single(c => c.coord == new Vector2Int(x + 5, 1)).type == TileType.PhaseSpikeDown,
                "Even-beat landing in sector " + sector);
        }
        Debug.Log("MATRIX_WEAVE_DEFINITION_PASSED");
    }

    // CLI: Unity -batchmode -projectPath <COPY> -executeMethod MatrixWeaveValidation.RunBatch
    public static void RunBatch()
    {
        try
        {
            ValidateDefinition();
            string output = ReadArgument("-matrixWeaveOutput") ?? Path.GetFullPath("MatrixWeaveResults");
            Directory.CreateDirectory(output);
            SessionState.SetString("MatrixWeave.Output", output);
            SessionState.SetString(StateKey, "preparing");
            EditorSceneManager.OpenScene("Assets/GameJamLevel  1.unity");
            CommandPanelRepair.SetupActiveSceneForLevel(8);
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/MatrixWeave.unity");
            AssetDatabase.SaveAssets();
            CaptureBoardPreview(output);
            AssetDatabase.ExportPackage(new[]
            {
                "Assets/Scenes/MatrixWeave.unity",
                "Assets/script/Managers/LevelDefinition.cs",
                "Assets/script/Editor/CommandPanelRepair.cs",
                "Assets/script/Editor/MatrixWeaveValidation.cs",
                "Assets/script/UI/UIManager.cs",
                "Assets/script/Tiles/IsometricBoardBuilder.cs",
                "Assets/script/Camera/GameCameraController.cs",
                "Assets/script/Editor/MatrixWeaveViewFix.cs"
            }, Path.Combine(output, "MatrixWeave.unitypackage"), ExportPackageOptions.IncludeDependencies);
            SessionState.SetString(StateKey, "ready");
            EditorApplication.isPlaying = true;
        }
        catch (Exception ex) { Finish(false, ex.ToString()); }
    }

    static void Tick()
    {
        string state = SessionState.GetString(StateKey, "");
        if (state != "ready" && state != "capture" && state != "running") return;
        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (state == "ready")
            {
                var lm = LevelManager.Instance;
                if (lm == null || lm.player == null || UIManager.Instance == null ||
                    UIManager.Instance.paletteCards.Count != 8) return;
                Check(lm.currentLevelIndex == 8 && TileManager.Instance.AllTiles.Count == 32,
                    "P9 loaded with32 runtime tiles");
                Check(UnityEngine.Object.FindObjectsByType<PhaseGateTile>(FindObjectsSortMode.None).Length == 3,
                    "Three runtime gates");
                Check(UnityEngine.Object.FindObjectsByType<PhaseSpikeTile>(FindObjectsSortMode.None).Length == 3,
                    "Three runtime phase landings");
                CheckCameraFits(lm);
                ScreenCapture.CaptureScreenshot(Path.Combine(SessionState.GetString("MatrixWeave.Output", ""),
                    "matrix-weave-in-game.png"));
                deadline = EditorApplication.timeSinceStartup + 1.5;
                SessionState.SetString(StateKey, "capture");
                return;
            }
            if (state == "capture")
            {
                if (EditorApplication.timeSinceStartup < deadline) return;
                steps = 0;
                failures = 0;
                Application.logMessageReceived += WatchLogs;
                var executor = UnityEngine.Object.FindFirstObjectByType<CodeExecutor>();
                executor.OnStepStarted += (_, __) => steps++;
                LevelManager.Instance.player.moveSpeed = 12f;
                CommandPanelRepair.TestRunL9();
                Check(UIManager.Instance.graphNodes.Count == 8 && UIManager.Instance.CommandCount == 8,
                    "All8 cards available, placed and wired through real UI");
                deadline = EditorApplication.timeSinceStartup + 60;
                SessionState.SetString(StateKey, "running");
                return;
            }
            var level = LevelManager.Instance;
            Check(failures == 0, "No runtime hazard deaths or exceptions");
            Check(EditorApplication.timeSinceStartup < deadline, "Runtime solution finishes before timeout");
            if (!level.IsLevelComplete) return;
            Check(steps == 24, "Three complete8-command passes");
            Check(level.player.GetCurrentTile().gridPosition == new Vector2Int(15, 0), "Exit reached");
            Check(!TileManager.Instance.IsGateFlipped, "Gate phase restored");
            Check(!UnityEngine.Object.FindFirstObjectByType<CodeExecutor>().IsRunning(), "Execution stopped at goal");
            UIManager.Instance.ResetProgram();
            Check(level.player.GetCurrentTile().gridPosition == Vector2Int.zero &&
                  TileManager.Instance.CurrentBeat == 0 && !TileManager.Instance.IsGateFlipped,
                "Retry resets robot, clock and gates");
            Check(UIManager.Instance.CommandCount == 8, "Retry preserves wired solution");
            Finish(true, "P9 reached exit in3 passes/24 commands with zero deaths; retry restored all state. P1-P8 preserved.");
        }
        catch (Exception ex) { Finish(false, ex.ToString()); }
    }

    static void CheckCameraFits(LevelManager lm)
    {
        var camera = Camera.main;
        var rig = camera.GetComponent<GameCameraController>();
        float originalAspect = camera.aspect;
        float originalSize = rig.orthoSize;
        bool customized = rig.userHasCustomized;
        // Verify actual platform/arch/pillar envelope, not just the center of each tile.
        foreach (float screenAspect in new[] { 16f / 9f, 16f / 10f, 4f / 3f })
        {
            camera.aspect = screenAspect / camera.rect.height;
            rig.userHasCustomized = true; // A previous user's close zoom must no longer bypass the fit.
            rig.orthoSize = 3.4f;
            rig.ApplyCameraSettings();
            foreach (float x in new[] { -1f, 31f })
            foreach (float y in new[] { -2.5f, 2.5f })
            foreach (float z in new[] { -1f, 3f })
            {
                var point = camera.WorldToViewportPoint(new Vector3(x, y, z));
                Check(point.x >= 0.04f && point.x <= 0.96f && point.y >= 0.05f && point.y <= 0.79f,
                    "Complete geometry inside safe viewport at aspect " + screenAspect + ": " + point);
                var backdrop = camera.transform.Find("FactoryBackdropQuad");
                Check(backdrop == null || point.z < backdrop.localPosition.z,
                    "Backdrop cannot occlude far platforms");
            }
        }
        camera.aspect = originalAspect;
        rig.orthoSize = originalSize;
        rig.userHasCustomized = customized;
        rig.ApplyCameraSettings();
        Debug.Log("MATRIX_WEAVE_FRAMING_PASSED: 16:9,16:10,4:3; pillars, arch, HUD margins and customized close zoom.");
    }

    static void CaptureBoardPreview(string output)
    {
        var camera = Camera.main;
        var rig = camera.GetComponent<GameCameraController>();
        var oldTarget = camera.targetTexture;
        var oldRect = camera.rect;
        float oldAspect = camera.aspect;
        float oldSize = rig.orthoSize;
        var texture = new RenderTexture(1920, 800, 24);
        var previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = texture;
            camera.aspect = 1920f / 800f;
            rig.ApplyCameraSettings();
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            camera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(1920, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 800), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(output, "matrix-weave-framing-preview.png"), image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            RenderTexture.active = previousActive;
            camera.targetTexture = oldTarget;
            camera.rect = oldRect;
            camera.aspect = oldAspect;
            rig.orthoSize = oldSize;
            rig.ApplyCameraSettings();
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    static void WatchLogs(string message, string trace, LogType type)
    {
        if (message.Contains("Respawning player") || type == LogType.Exception || type == LogType.Error)
            failures++;
    }

    static void Finish(bool success, string detail)
    {
        SessionState.SetString(StateKey, "");
        Application.logMessageReceived -= WatchLogs;
        string output = SessionState.GetString("MatrixWeave.Output", "");
        string report = (success ? "MATRIX_WEAVE_RUNTIME_PASSED" : "MATRIX_WEAVE_FAILED") + "\n" + detail;
        Debug.Log(report);
        if (!string.IsNullOrEmpty(output)) File.WriteAllText(Path.Combine(output, "matrix-weave-validation.txt"), report);
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
    }

    static string ReadArgument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Matrix Weave: " + message);
    }
}
#endif
