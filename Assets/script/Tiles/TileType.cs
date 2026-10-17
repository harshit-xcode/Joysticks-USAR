// TileType.cs
// Defines all static and dynamic tile types used in the 3D isometric game grid.

public enum TileType
{
    Start = 0,           // Player spawns here at the beginning or after respawn
    Free = 1,            // Safe tile — player can walk or land on it
    Lava = 2,            // Static hazard tile — player respawns if landed on
    End = 3,             // Goal tile — player wins the level
    Obstacle = 4,        // Solid barrier tile — blocks movement & jump-through (enables loop asymmetry!)
    PhaseSpikeUp = 5,    // Dynamic hydraulic spikes: UP (lethal) on even beats (0,2,4...), DOWN (safe) on odd beats (1,3,5...)
    PhaseSpikeDown = 6,  // Dynamic hydraulic spikes: DOWN (safe) on even beats (0,2,4...), UP (lethal) on odd beats (1,3,5...)
    PhaseGateClosed = 7, // Dynamic laser barrier: Starts CLOSED (blocks like Obstacle), toggles OPEN when [PULSE] fires!
    PhaseGateOpen = 8    // Dynamic laser barrier: Starts OPEN (walkable), toggles CLOSED (blocks like Obstacle) when [PULSE] fires!
}
