// GameManager.cs
// Top-level game state controller. Persists across scenes using DontDestroyOnLoad.
// Tracks global game state (main menu, playing, paused, game over).
// Add more global logic here as your game grows (e.g. level progress, settings).

using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,   // Player is on the main menu
    Playing,    // A level is actively being played
    Paused,     // Game is paused mid-level
    GameOver    // Player has quit or all lives lost (expandable for future)
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState currentState = GameState.MainMenu;

    private void Awake()
    {
        // Singleton — persist across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Call this when starting a new game / entering a level.
    public void StartGame()
    {
        currentState = GameState.Playing;
        Debug.Log("GameManager: Game started.");

        // Load Level 1 (scene index 1 — ensure it's added to Build Settings)
        SceneManager.LoadScene(1);
    }

    // Pauses the game (freezes time).
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Paused;
        Time.timeScale = 0f;
        Debug.Log("GameManager: Game paused.");
    }

    // Resumes the game from pause.
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        Debug.Log("GameManager: Game resumed.");
    }

    // Returns to the main menu.
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        currentState = GameState.MainMenu;
        SceneManager.LoadScene(0); // Scene 0 = Main Menu
        Debug.Log("GameManager: Returned to Main Menu.");
    }
}
