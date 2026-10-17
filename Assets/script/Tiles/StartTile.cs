// StartTile.cs
// The tile where the player begins each level.
// PlayerRespawn also returns the player to this tile.

using UnityEngine;

public class StartTile : Tile
{
    private void Awake()
    {
        tileType = TileType.Start;
    }

    // Nothing special happens when the player steps on the start tile.
    // It is mainly used as the respawn anchor point.
    public override void OnPlayerEnter(PlayerController player)
    {
        // No action needed on start tile entry.
        Debug.Log("Player is on the Start tile.");
    }
}
