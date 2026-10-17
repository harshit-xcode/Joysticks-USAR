// TileManager.cs
// Manages the entire tile grid for a level.
// Stores all tiles in a dictionary for fast lookup by grid position.
// Also tracks the Start tile so the player can respawn there.

using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    // All tiles in the level, indexed by their grid position
    private Dictionary<Vector2Int, Tile> tileGrid = new Dictionary<Vector2Int, Tile>();

    // Reference to the start tile (used for respawning)
    private StartTile startTile;

    // Awake runs before any Start() — so tiles are registered
    // before LevelManager.Start() tries to access them.
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Register all tiles immediately
        RegisterAllTiles();
    }

    // Finds all Tile components in the scene and registers them in the grid dictionary.
    // Called once at level load via Awake().
    private void RegisterAllTiles()
    {
        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);

        foreach (Tile tile in allTiles)
        {
            tileGrid[tile.gridPosition] = tile;

            // Cache the start tile separately
            if (tile is StartTile st)
            {
                startTile = st;
            }
        }

        Debug.Log($"TileManager: Registered {tileGrid.Count} tiles.");
    }

    // Returns the tile at the given grid position, or null if none exists.
    public Tile GetTile(Vector2Int position)
    {
        tileGrid.TryGetValue(position, out Tile tile);
        return tile;
    }

    // Returns the Start tile (used for player respawn).
    public StartTile GetStartTile()
    {
        return startTile;
    }
}
