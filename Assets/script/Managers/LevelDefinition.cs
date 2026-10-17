// LevelDefinition.cs
// Data-driven level configuration and 5-level puzzle campaign catalog.
// Built around true 2-tile JUMP (vaulting from tile X to X+2 over X+1) and Obstacle Asymmetry.

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct NodeLoadoutItem
{
    public BlockType nodeType;
    [Min(1)] public int count;

    public NodeLoadoutItem(BlockType type, int count = 1)
    {
        this.nodeType = type;
        this.count = count;
    }
}

[Serializable]
public struct TileCellData
{
    public Vector2Int coord;
    public TileType type;
    public bool hasObstacle;

    public TileCellData(int x, int y, TileType type, bool hasObstacle = false)
    {
        this.coord = new Vector2Int(x, y);
        this.type = type;
        this.hasObstacle = hasObstacle;
    }
}

[Serializable]
public class LevelDefinition
{
    public int levelIndex = 1;
    public string title = "PROTOCOL 01 // CLOSED LOOP";
    public string objectiveHint = "DRAG [WALK] TO THE BOARD & MANUALLY WIRE START -> WALK -> LOOP";
    public List<NodeLoadoutItem> unlockedNodes = new List<NodeLoadoutItem>();
    public List<TileCellData> cells = new List<TileCellData>();
}

public static class LevelCampaignCatalog
{
    public static List<LevelDefinition> CreateDefault7Levels()
    {
        return CreateCampaignLevels();
    }

    public static List<LevelDefinition> CreateCampaignLevels()
    {
        var levels = new List<LevelDefinition>();

        // =====================================================================
        // LEVEL 1: PROTOCOL 01 // CLOSED LOOP
        // Unlocks: WALK (+1 tile)
        // Solution: START -> WALK -> LOOP
        // =====================================================================
        var lvl1 = new LevelDefinition
        {
            levelIndex = 1,
            title = "PROTOCOL 01 // CLOSED LOOP",
            objectiveHint = "DRAG [WALK] TO THE CIRCUIT BOARD, THEN DRAG PORTS TO WIRE: START -> WALK -> LOOP"
        };
        lvl1.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Walk, 2));
        lvl1.cells.Add(new TileCellData(0, 0, TileType.Start));
        lvl1.cells.Add(new TileCellData(1, 0, TileType.Free));
        lvl1.cells.Add(new TileCellData(2, 0, TileType.Free));
        lvl1.cells.Add(new TileCellData(3, 0, TileType.Free));
        lvl1.cells.Add(new TileCellData(4, 0, TileType.End));
        levels.Add(lvl1);

        // =====================================================================
        // LEVEL 2: PROTOCOL 02 // PHASE LEAP
        // Unlocks: JUMP (Leaps from tile X to X+2, skipping X+1!)
        // Order matters: WALK -> JUMP vaults over Lava at X=2 and X=5,
        // whereas JUMP -> WALK lands directly on X=2 Lava!
        // Solution: START -> WALK -> JUMP -> LOOP
        // =====================================================================
        var lvl2 = new LevelDefinition
        {
            levelIndex = 2,
            title = "PROTOCOL 02 // PHASE LEAP",
            objectiveHint = "NEW MODULE: [JUMP] LEAPS OVER 1 TILE (1 -> 3). ORDER YOUR LOOP TO SKIP THE SPIKES!"
        };
        lvl2.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Walk, 2));
        lvl2.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Lava, 2));
        lvl2.cells.Add(new TileCellData(0, 0, TileType.Start));
        lvl2.cells.Add(new TileCellData(1, 0, TileType.Free));
        lvl2.cells.Add(new TileCellData(2, 0, TileType.Lava));
        lvl2.cells.Add(new TileCellData(3, 0, TileType.Free));
        lvl2.cells.Add(new TileCellData(4, 0, TileType.Free));
        lvl2.cells.Add(new TileCellData(5, 0, TileType.Lava));
        lvl2.cells.Add(new TileCellData(6, 0, TileType.End));
        levels.Add(lvl2);

        // =====================================================================
        // LEVEL 3: PROTOCOL 03 // WALL CLAMP
        // Unlocks: LEFT + Obstacle Asymmetry (blocked moves stay in place!)
        // Solution: START -> WALK -> LEFT -> LOOP
        // =====================================================================
        var lvl3 = new LevelDefinition
        {
            levelIndex = 3,
            title = "PROTOCOL 03 // WALL CLAMP",
            objectiveHint = "NEW MODULE: [LEFT] — BLOCKED MOVES DO NOTHING! USE WALLS FOR LOOP ASYMMETRY"
        };
        lvl3.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Walk, 2));
        lvl3.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveLeft, 1));
        lvl3.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Lava, 1));
        lvl3.cells.Add(new TileCellData(0, 0, TileType.Start));
        lvl3.cells.Add(new TileCellData(1, 0, TileType.Free));
        lvl3.cells.Add(new TileCellData(2, 0, TileType.Free));
        lvl3.cells.Add(new TileCellData(3, 0, TileType.Obstacle, true)); // Clamps forward move at (2,0)
        lvl3.cells.Add(new TileCellData(0, 1, TileType.Obstacle, true)); // Clamps early left at (0,0)
        lvl3.cells.Add(new TileCellData(1, 1, TileType.Obstacle, true)); // Clamps early left at (1,0)
        lvl3.cells.Add(new TileCellData(2, 1, TileType.Free));           // Open left turn at (2,0)
        lvl3.cells.Add(new TileCellData(3, 1, TileType.Free));
        lvl3.cells.Add(new TileCellData(4, 1, TileType.End));
        levels.Add(lvl3);

        // =====================================================================
        // LEVEL 4: PROTOCOL 04 // PHASE WEAVE (Grand Finale)
        // Unlocks: RIGHT (All 4 modules: 1x JUMP, 1x WALK, 1x LEFT, 1x RIGHT)
        // Designed with 2 Intuitive Traps + 1 "AHA!" Epiphany:
        // - Intuitive Trap #1 (Linear Fork Read: J -> W -> L or J -> W -> R):
        //   Reading Pass 1 linearly takes the Upper Bridge (J->W->L, blocked at (7,1) on Pass 3)
        //   or Lower Bridge (J->W->R, blocked at (6,-1) on Pass 2). Appending the opposite turn
        //   at the end (J->W->L->R or J->W->R->L) cancels out at (3,0) on Pass 1!
        // - Intuitive Trap #2 (Upper Bridge Weave: J -> R -> W -> L):
        //   Separating R and L with W so R is blocked at (2,-1) on Pass 1 and merges (5,1)->(5,0)
        //   on Pass 2 almost works — until L at (6,0) pulls the robot back into (6,1) Free!
        // - The 3rd "AHA!" Solution (START -> JUMP -> LEFT -> WALK -> RIGHT -> LOOP):
        //   Flipping the weave to (J -> L -> W -> R) uses (2,1) Obstacle to block L on Pass 1,
        //   merges (5,-1)->(5,0) via L on Pass 2, and uses the (6,-1) Obstacle as a backstop
        //   to block R at (6,0) so Pass 3 can leap (6,0)->(8,0), step L to (8,1), and W to (9,1)!
        // =====================================================================
        var lvl4 = new LevelDefinition
        {
            levelIndex = 4,
            title = "PROTOCOL 04 // PHASE WEAVE",
            objectiveHint = "FINAL TEST — TWO BRIDGES ARE TRAPS. USE GATE & BACKSTOP WALLS TO WEAVE ALL 4 MODULES!"
        };
        lvl4.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Lava, 1));      // 1x JUMP
        lvl4.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Walk, 1));      // 1x WALK
        lvl4.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveLeft, 1));  // 1x LEFT
        lvl4.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveRight, 1)); // 1x RIGHT

        // Start & Pass 1 Corridor (y = 0)
        lvl4.cells.Add(new TileCellData(0, 0, TileType.Start));
        lvl4.cells.Add(new TileCellData(0, -1, TileType.Lava));           // Blocks R-first cyclic rotation at (0,0)
        lvl4.cells.Add(new TileCellData(1, 0, TileType.Lava));            // Pass 1 JUMP gap: (0,0) -> (2,0)
        lvl4.cells.Add(new TileCellData(2, 0, TileType.Free));
        lvl4.cells.Add(new TileCellData(2, 1, TileType.Obstacle, true));  // Gate Wall Left: clamps L at (2,0) on Pass 1
        lvl4.cells.Add(new TileCellData(2, -1, TileType.Obstacle, true)); // Gate Wall Right: clamps R at (2,0) on Pass 1
        lvl4.cells.Add(new TileCellData(3, 0, TileType.Free));
        lvl4.cells.Add(new TileCellData(4, 0, TileType.Obstacle, true));  // Central Divider Pillar at (4,0)

        // Upper Bridge (y = +1): lures J->W->L and betrays J->R->W->L via open (6,1) tile!
        lvl4.cells.Add(new TileCellData(3, 1, TileType.Free));
        lvl4.cells.Add(new TileCellData(4, 1, TileType.Lava));
        lvl4.cells.Add(new TileCellData(5, 1, TileType.Free));
        lvl4.cells.Add(new TileCellData(6, 1, TileType.Free));
        lvl4.cells.Add(new TileCellData(7, 1, TileType.Obstacle, true));  // Upper Bridge dead-end wall at (7,1)

        // Lower Bridge (y = -1): (6,-1) Obstacle blocks J->W->R, but acts as backstop for J->L->W->R at (6,0)!
        lvl4.cells.Add(new TileCellData(3, -1, TileType.Free));
        lvl4.cells.Add(new TileCellData(4, -1, TileType.Lava));
        lvl4.cells.Add(new TileCellData(5, -1, TileType.Free));
        lvl4.cells.Add(new TileCellData(6, -1, TileType.Obstacle, true)); // Backstop wall at (6,-1)

        // Center Re-entry & Pass 3 Finale Corridor
        lvl4.cells.Add(new TileCellData(5, 0, TileType.Free));
        lvl4.cells.Add(new TileCellData(6, 0, TileType.Free));
        lvl4.cells.Add(new TileCellData(7, 0, TileType.Lava));            // Pass 3 JUMP gap: (6,0) -> (8,0)
        lvl4.cells.Add(new TileCellData(8, 0, TileType.Free));
        lvl4.cells.Add(new TileCellData(9, 0, TileType.Obstacle, true));  // Forces L at (8,0)->(8,1) before W->(9,1)
        lvl4.cells.Add(new TileCellData(8, 1, TileType.Free));
        lvl4.cells.Add(new TileCellData(9, 1, TileType.End));
        levels.Add(lvl4);

        return levels;
    }
}
