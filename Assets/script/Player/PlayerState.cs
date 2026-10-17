// PlayerState.cs
// Defines all possible states the player (robot) can be in.

public enum PlayerState
{
    Idle,    // Standing still, waiting for code to run
    Moving,  // Currently walking to the next tile
    Dead,    // Hit lava — being respawned
    Win      // Reached the end tile — level complete
}
