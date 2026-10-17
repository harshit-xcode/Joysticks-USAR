// FreeTile.cs
// A safe tile the player can walk on freely.
// No special action happens when the player steps on this tile.

using UnityEngine;

public class FreeTile : Tile
{
    private void Awake()
    {
        tileType = TileType.Free;
    }

    // Player steps on this tile — nothing happens.
    public override void OnPlayerEnter(PlayerController player)
    {
        // Safe tile — no action.
        Debug.Log("Player is on a Free tile.");
    }
}
