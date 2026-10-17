// EndTile.cs
// The goal tile. When the player reaches this tile, the level is won.

using UnityEngine;

public class EndTile : Tile
{
    private void Awake()
    {
        tileType = TileType.End;
    }

    // Player reaches the end tile — trigger win condition.
    public override void OnPlayerEnter(PlayerController player)
    {
        Debug.Log("Player reached the End tile! Level Complete!");
        LevelManager.Instance.Win();
    }
}
