// ObstacleTile.cs
// A solid barrier/pillar tile that blocks movement into its cell.
// Used to create asymmetric loop behavior (e.g., Walk/Jump forward do nothing when blocked,
// allowing the next Left/Right node in the loop to sidestep the obstacle).

using UnityEngine;

public class ObstacleTile : Tile
{
    private void Awake()
    {
        tileType = TileType.Obstacle;
        hasObstacle = true;
    }

    public override bool CanPlayerEnter(PlayerController player)
    {
        return false;
    }

    public override void OnPlayerEnter(PlayerController player)
    {
        // Players cannot enter an ObstacleTile.
    }
}
