// EndTile.cs
// The goal deployment gate tile. When the player reaches this tile, the level is won.

using UnityEngine;

public class EndTile : Tile
{
    private void Awake()
    {
        tileType = TileType.End;
    }

    public override void OnPlayerEnter(PlayerController player)
    {
        Debug.Log("Player reached the End tile! Level Complete!");
        AudioManager.Instance?.PlayWin();
        if (LevelManager.Instance != null)
            LevelManager.Instance.Win();
    }
}
