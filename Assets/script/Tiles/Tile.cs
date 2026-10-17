// Tile.cs
// Abstract base class for all tile types in the 3D isometric grid.

using UnityEngine;

public abstract class Tile : MonoBehaviour
{
    [Header("Tile Settings")]
    public TileType tileType;
    public Vector2Int gridPosition;

    [Header("Obstacle / Asymmetry Settings")]
    [Tooltip("If true, a physical obstacle/barrier sits on this tile, blocking movement into it.")]
    public bool hasObstacle = false;
    public GameObject obstacleVisual;

    // Checks whether the robot can move onto this tile.
    // Blocked tiles cause movement nodes (Walk, Jump, Left, Right, Force) to do nothing (bump).
    public virtual bool CanPlayerEnter(PlayerController player)
    {
        return !hasObstacle && tileType != TileType.Obstacle;
    }

    // Called by PlayerController when the player steps onto this tile.
    public abstract void OnPlayerEnter(PlayerController player);
}
