// LevelManager.cs
// Manages the state of the current level and the 7-level scalable campaign progression.
// Supports both Edit-Mode scene-authored boards and dynamic multi-level transitions.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Core References")]
    public PlayerController player;
    public UIManager uiManager;
    public Transform boardRoot;
    public float cellSpacing = 2.0f;

    [Header("7-Level Scalable Campaign")]
    [Min(0)] public int currentLevelIndex = 0;
    public List<LevelDefinition> campaignLevels = new List<LevelDefinition>();

    [Header("Optional Custom 3D Prefab & Backdrop Overrides (Drop Custom Assets Here)")]
    public Texture2D customBackdropTexture;
    public GameObject customStartTilePrefab;
    public GameObject customFreeTilePrefab;
    public GameObject customLavaTilePrefab;
    public GameObject customObstacleTilePrefab;
    public GameObject customEndTilePrefab;
    public GameObject customRobotVisualPrefab;

    private bool levelWon = false;
    public bool IsLevelComplete => levelWon;

    public LevelDefinition CurrentLevel
    {
        get
        {
            EnsureCampaignInitialized();
            int clamped = Mathf.Clamp(currentLevelIndex, 0, campaignLevels.Count - 1);
            return campaignLevels[clamped];
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureCampaignInitialized();
    }

    public void EnsureCampaignInitialized()
    {
        if (campaignLevels == null || campaignLevels.Count != 7 || campaignLevels[6].title != "PROTOCOL 07 // DYNAMIC MATRIX")
        {
            campaignLevels = LevelCampaignCatalog.CreateCampaignLevels();
        }
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        if (boardRoot == null)
        {
            var existingBoard = GameObject.Find("GameObject");
            if (existingBoard == null) existingBoard = GameObject.Find("IsometricBoard");
            if (existingBoard != null) boardRoot = existingBoard.transform;
        }

        // Ensure robot has its 3D silhouette visual and is placed on Start
        if (player != null)
        {
            IsometricBoardBuilder.StyleRobot(player, customRobotVisualPrefab);
        }

        LoadCampaignLevel(currentLevelIndex, true);
    }

    public void LoadCampaignLevel(int index, bool rebuildBoard = true)
    {
        EnsureCampaignInitialized();
        currentLevelIndex = Mathf.Clamp(index, 0, campaignLevels.Count - 1);
        levelWon = false;

        var executor = FindFirstObjectByType<CodeExecutor>();
        executor?.StopExecution();

        LevelDefinition level = campaignLevels[currentLevelIndex];

        if (rebuildBoard && boardRoot != null)
        {
            IsometricBoardBuilder.BuildLevelOnBoard(
                boardRoot,
                level,
                cellSpacing,
                customStartTilePrefab,
                customFreeTilePrefab,
                customLavaTilePrefab,
                customObstacleTilePrefab,
                customEndTilePrefab);
        }

        if (TileManager.Instance != null)
        {
            TileManager.Instance.cellSpacing = cellSpacing;
            TileManager.Instance.RegisterAllTiles();
        }

        if (Camera.main != null)
        {
            IsometricBoardBuilder.FrameCameraForLevel(Camera.main, level, cellSpacing, customBackdropTexture);
        }

        if (player != null)
        {
            player.GetComponent<PlayerRespawn>()?.CancelPendingRespawn();
            player.PlaceOnStartTile();
        }

        if (uiManager == null)
            uiManager = UIManager.Instance != null ? UIManager.Instance : FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ConfigureForLevel(level, currentLevelIndex, campaignLevels.Count);
        }

        Debug.Log($"LevelManager: Loaded {level.title} (Level {currentLevelIndex + 1}/{campaignLevels.Count}).");
    }

    public void Win()
    {
        if (levelWon) return;

        levelWon = true;
        if (player != null) player.currentState = PlayerState.Win;

        CodeExecutor executor = FindFirstObjectByType<CodeExecutor>();
        executor?.CompleteLevel();

        Debug.Log($"LevelManager: {CurrentLevel.title} Complete!");

        if (uiManager != null)
        {
            bool hasNext = currentLevelIndex + 1 < campaignLevels.Count;
            uiManager.ShowVictoryBanner(CurrentLevel, hasNext);
        }
    }

    public void RestartLevel() { ResetLevel(true); }

    public void ResetForRetry() { ResetLevel(false); }

    void ResetLevel(bool clearProgram)
    {
        levelWon = false;
        var executor = FindFirstObjectByType<CodeExecutor>();
        executor?.StopExecution();
        if (player != null)
        {
            player.GetComponent<PlayerRespawn>()?.CancelPendingRespawn();
            player.PlaceOnStartTile();
        }
        if (uiManager != null)
        {
            uiManager.HideVictoryBanner();
            if (clearProgram) uiManager.ClearAllSlots();
        }
        Debug.Log("Level reset. Robot is back at Start.");
    }

    public void LoadNextLevel()
    {
        EnsureCampaignInitialized();
        if (currentLevelIndex + 1 < campaignLevels.Count)
        {
            LoadCampaignLevel(currentLevelIndex + 1, true);
        }
        else
        {
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex > 0 && nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.Log("LevelManager: All 4 Chrono-Loop Protocols complete! Looping back to Protocol 01.");
                LoadCampaignLevel(0, true);
            }
        }
    }
}
