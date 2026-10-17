// LavaTile.cs
// Hazard tile (Red Spikes). Any robot that lands ON this tile is destroyed and respawns at Start.
// To cross a LavaTile safely, the robot must JUMP OVER it from the tile before it (1 -> 3, skipping 2).

using UnityEngine;

public class LavaTile : Tile
{
    private void Awake()
    {
        tileType = TileType.Lava;
    }

    public override void OnPlayerEnter(PlayerController player)
    {
        Debug.Log($"Player landed on Hazard tile at {gridPosition}! Respawning...");
        AudioManager.Instance?.PlayLavaFail();
        var respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null) respawn.Respawn();
    }
}
