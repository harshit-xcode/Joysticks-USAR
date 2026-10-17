// TileManager.cs
// Manages the entire 3D isometric tile grid for the active level.
// Supports both scene-authored tiles and data-driven level generation.

using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("World-space distance between adjacent grid cells.")]
    public float cellSpacing = 2.0f;

    private readonly Dictionary<Vector2Int, Tile> tileGrid = new Dictionary<Vector2Int, Tile>();
    private StartTile startTile;
    private EndTile endTile;

    public IReadOnlyDictionary<Vector2Int, Tile> AllTiles => tileGrid;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RegisterAllTiles();
    }

    // Scans all active Tile components in the scene and indexes them by gridPosition.
    public void RegisterAllTiles()
    {
        tileGrid.Clear();
        startTile = null;
        endTile = null;

        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (Tile tile in allTiles)
        {
            if (tile == null || !tile.gameObject.activeInHierarchy) continue;

            tileGrid[tile.gridPosition] = tile;

            if (tile is StartTile st) startTile = st;
            else if (tile is EndTile et) endTile = et;
        }

        Debug.Log($"TileManager: Registered {tileGrid.Count} tiles.");
    }

    public Tile GetTile(Vector2Int position)
    {
        if (tileGrid.Count == 0) RegisterAllTiles();
        tileGrid.TryGetValue(position, out Tile tile);
        return tile;
    }

    public StartTile GetStartTile()
    {
        if (startTile == null) RegisterAllTiles();
        return startTile;
    }

    public EndTile GetEndTile()
    {
        if (endTile == null) RegisterAllTiles();
        return endTile;
    }

    public Vector3 GridToWorld(Vector2Int gridPos, float yOffset = 0f)
    {
        Tile t = GetTile(gridPos);
        if (t != null)
            return t.transform.position + Vector3.up * yOffset;

        Vector3 origin = startTile != null
            ? startTile.transform.position - new Vector3(startTile.gridPosition.x * cellSpacing, 0f, startTile.gridPosition.y * cellSpacing)
            : Vector3.zero;
        return origin + new Vector3(gridPos.x * cellSpacing, yOffset, gridPos.y * cellSpacing);
    }
}
