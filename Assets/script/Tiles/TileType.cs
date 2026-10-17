// TileType.cs
// Defines all tile types used in the isometric game grid.

public enum TileType
{
    Start = 0,    // Player spawns here at the beginning or after respawn
    Free = 1,     // Safe tile — player can walk or land on it
    Lava = 2,     // Hazard tile — player respawns if entered without JUMP (LavaBlock)
    End = 3,      // Goal tile — player wins the level
    Obstacle = 4  // Solid barrier tile — blocks movement into this cell (enables loop asymmetry!)
}
