// LavaTile.cs
// Danger tile. If the player steps on this WITHOUT using a LavaBlock
// in their code sequence, they are sent back to the Start tile.
// If the player used a LavaBlock, the isCrossingLava flag is true
// and the lava is crossed safely.

using UnityEngine;

public class LavaTile : Tile
{
    private void Awake()
    {
        tileType = TileType.Lava;
    }

    // Called when the player enters this tile.
    // Checks if the player is safely crossing lava via LavaBlock.
    public override void OnPlayerEnter(PlayerController player)
    {
        if (player.isCrossingLava)
        {
            // Player used the LavaBlock — safe crossing
            Debug.Log("Player crossed the Lava tile safely!");
        }
        else
        {
            // Player walked onto lava without LavaBlock — respawn
            Debug.Log("Player touched Lava! Respawning...");
            player.GetComponent<PlayerRespawn>().Respawn();
        }
    }
}
