// TileType.cs
// Defines all tile types used in the game grid.

public enum TileType
{
    Start,  // Player spawns here at the beginning or after respawn
    Free,   // Safe tile — nothing happens when player steps on it
    Lava,   // Danger tile — player respawns to Start if touched without LavaBlock
    End     // Goal tile — player wins the level
}
