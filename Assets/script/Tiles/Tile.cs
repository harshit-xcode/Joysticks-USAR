// Tile.cs
// Abstract base class for all tile types.
// Every tile in the game grid inherits from this.

using UnityEngine;

public abstract class Tile : MonoBehaviour
{
    [Header("Tile Settings")]
    public TileType tileType;           // What kind of tile this is
    public Vector2Int gridPosition;     // Position of this tile in the grid (column, row)

    // Called by PlayerController when the player steps onto this tile.
    // Each tile subclass overrides this to define its own behaviour.
    public abstract void OnPlayerEnter(PlayerController player);
}
