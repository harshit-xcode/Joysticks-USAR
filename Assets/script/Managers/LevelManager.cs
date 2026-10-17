// LevelManager.cs
// Manages the state of the current level.
// Handles win condition (player reaches End tile) and
// level restart (called after respawn or manually by the player).

using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("References")]
    public PlayerController player;       // The robot player
    public UIManager uiManager;           // The coding interface

    // Has the level been won?
    private bool levelWon = false;
    public bool IsLevelComplete => levelWon;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Place the player on the start tile at the beginning of the level
        player.PlaceOnStartTile();
    }

    // Called by EndTile when the player steps on it.
    // Triggers the win state for the level.
    public void Win()
    {
        if (levelWon) return;    // Prevent triggering win multiple times

        levelWon = true;
        player.currentState = PlayerState.Win;

        // Stop any running code
        CodeExecutor executor = FindFirstObjectByType<CodeExecutor>();
        executor?.CompleteLevel();

        Debug.Log("LevelManager: Level complete! 🎉");

        // TODO: Show win UI panel / trigger win animation here
        // For now, log to console. You can hook up a Win panel in the Inspector.
    }

    // Restarts the current level from scratch.
    // Resets player position, clears slots, reloads state.
    public void RestartLevel() { ResetLevel(true); }

    // The RESET button returns the robot to Start without deleting the user's program.
    public void ResetForRetry() { ResetLevel(false); }

    void ResetLevel(bool clearProgram)
    {
        levelWon = false;
        var executor = FindFirstObjectByType<CodeExecutor>();
        executor?.StopExecution();
        if (player != null)
        {
            // A delayed lava respawn must not teleport the robot during a new run.
            player.GetComponent<PlayerRespawn>()?.CancelPendingRespawn();
            player.PlaceOnStartTile();
        }
        if (clearProgram && uiManager != null) uiManager.ClearAllSlots();
        Debug.Log("Level reset. Robot is back at Start.");
    }

    // Loads the next level scene by build index.
    // Call this from your Win UI button.
    public void LoadNextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("LevelManager: No more levels — you've completed the game!");
            // TODO: Show end-game / credits screen
        }
    }
}
