// LevelDefinition.cs
// Data-driven level configuration and 9-level puzzle campaign catalog.
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

// Purely visual role highlight drawn on a tile (rim color + floor symbol). Does not change gameplay.
public enum TileAccent
{
    None = 0,
    Start = 1,   // Cyan rim
    Walk = 2,    // Cyan rim + forward (+X) arrow
    Gate = 3,    // Amber rim
    Rotate = 4,  // Purple rim + clockwise arrow
    Jump = 5,    // Yellow rim + jump arc
    End = 6      // Cyan rim + cyan goal pad
}

[Serializable]
public struct TileCellData
{
    public Vector2Int coord;
    public TileType type;
    public bool hasObstacle;
    public TileAccent accent;

    public TileCellData(int x, int y, TileType type, bool hasObstacle = false, TileAccent accent = TileAccent.None)
    {
        this.coord = new Vector2Int(x, y);
        this.type = type;
        this.hasObstacle = hasObstacle;
        this.accent = accent;
    }
}

[Serializable]
public class LevelDefinition
{
    public int levelIndex = 1;
    public string title = "L1. First Loop";
    public string objectiveHint = "DRAG [WALK] TO THE BOARD & MANUALLY WIRE START -> WALK -> LOOP";
    public List<NodeLoadoutItem> unlockedNodes = new List<NodeLoadoutItem>();
    public List<TileCellData> cells = new List<TileCellData>();
    public bool fitCameraToBoard = false;
}

public static class LevelCampaignCatalog
{
    // Keep in sync with CreateCampaignLevels(): used to detect stale serialized campaigns and size the P1..Pn selector.
    public const int CampaignLevelCount = 9;
    public const string FinalLevelTitle = "L9. Final Weave";

    public static List<LevelDefinition> CreateDefault7Levels()
    {
        return CreateCampaignLevels();
    }

    public static List<LevelDefinition> CreateCampaignLevels()
    {
        var levels = new List<LevelDefinition>();

        // =====================================================================
        // LEVEL 1: L1. First Loop
        // Unlocks: WALK (+1 tile)
        // Solution: START -> WALK -> LOOP
        // =====================================================================
        var lvl1 = new LevelDefinition
        {
            levelIndex = 1,
            title = "L1. First Loop",
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
        // LEVEL 2: L2. Spike Hop
        // Unlocks: JUMP (Leaps from tile X to X+2, skipping X+1!)
        // Order matters: WALK -> JUMP vaults over Lava at X=2 and X=5,
        // whereas JUMP -> WALK lands directly on X=2 Lava!
        // Solution: START -> WALK -> JUMP -> LOOP
        // =====================================================================
        var lvl2 = new LevelDefinition
        {
            levelIndex = 2,
            title = "L2. Spike Hop",
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
        // LEVEL 3: L3. Wall Trick
        // Unlocks: LEFT + Obstacle Asymmetry (blocked moves stay in place!)
        // Solution: START -> WALK -> LEFT -> LOOP
        // =====================================================================
        var lvl3 = new LevelDefinition
        {
            levelIndex = 3,
            title = "L3. Wall Trick",
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
        // LEVEL 4: L4. Lane Weaver (Grand Finale)
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
            title = "L4. Lane Weaver",
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

        // =====================================================================
        // LEVEL 5: L5. Power Pulse (Act II Introduction)
        // Unlocks: PULSE (BlockType.Wait) + Dynamic Laser Phase Gates + Hydraulic Phase Spikes
        // - [PULSE] holds position for 1 beat, emits an EMP shockwave, and flips all Laser Gates!
        // - Hydraulic Phase Spikes alternate UP (Lethal) <-> DOWN (Safe) on EVERY beat!
        // Unique Solution (1/15): START -> PULSE -> JUMP -> WALK -> LOOP
        // =====================================================================
        var lvl5 = new LevelDefinition
        {
            levelIndex = 5,
            title = "L5. Power Pulse",
            objectiveHint = "NEW MODULE: [PULSE] FLIPS LASER GATES & ADVANCES THE BEAT CLOCK. WATCH THE HYDRAULIC SPIKES!"
        };
        lvl5.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Wait, 1)); // 1x PULSE
        lvl5.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Lava, 1)); // 1x JUMP
        lvl5.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Walk, 1)); // 1x WALK

        lvl5.cells.Add(new TileCellData(0, 0, TileType.Start));
        lvl5.cells.Add(new TileCellData(1, 0, TileType.Lava));
        lvl5.cells.Add(new TileCellData(2, 0, TileType.PhaseGateClosed, true)); // Opens when PULSE fires!
        lvl5.cells.Add(new TileCellData(3, 0, TileType.Free));
        lvl5.cells.Add(new TileCellData(4, 0, TileType.Lava));
        lvl5.cells.Add(new TileCellData(5, 0, TileType.PhaseSpikeDown));        // Safe on even beats (Beat 4), Lethal on odd beats!
        lvl5.cells.Add(new TileCellData(6, 0, TileType.End));
        levels.Add(lvl5);

        // =====================================================================
        // LEVEL 6: L6. Beat Breaker
        // Uses ALL 5 MODULES (1x JUMP, 1x LEFT, 1x PULSE, 1x WALK, 1x RIGHT).
        // Engineered with 2 Intuitive Traps + 1 "AHA!" Epiphany:
        // - Intuitive Trap #1 (Early Pulse at (2,0): J -> P -> L -> W -> R or J -> P -> W -> R -> L):
        //   Firing PULSE right after JUMP at (2,0) opens (3,0) Gate immediately, tempting a linear entry.
        //   If L is before W, L bumps (2,1) wall, W enters (3,0), R enters (3,-1), and Pass 2 JUMP lands
        //   on (5,-1) PhaseSpikeUp on Beat 5 (ODD beat -> LETHAL!). If W is before R->L, R and L cancel at (3,0)!
        // - Intuitive Trap #2 (Upper Bridge + Pulse Weave: J -> R -> P -> W -> L):
        //   Taking the Upper Bridge (3,1)->(5,1) and using PULSE to open (3,0) looks promising until Pass 2:
        //   PULSE at (5,0) closes (6,1) PhaseGateOpen, so when L fires at (6,0), the closed (6,1) gate
        //   clamps L — and then Pass 3 JUMP leaps (6,0)->(8,0) onto PhaseSpikeDown on Beat 10 (EVEN),
        //   R stays at (8,0), PULSE flips (9,1) open on Beat 12, W steps into (9,0) End? No — (6,1) is
        //   actually OPEN on Beat 9 (since PULSE fired twice: Beat 2 & Beat 7!), so L at (6,0) steps into
        //   the open (6,1) gate and gets trapped against (7,1) wall!
        // - The 3rd "AHA!" Solution (START -> JUMP -> LEFT -> PULSE -> WALK -> RIGHT -> LOOP):
        //   By placing [PULSE] BETWEEN [LEFT] and [WALK] (`J -> L -> P -> W -> R`):
        //   * Pass 1: J(0,0->2,0), L bumps (2,1) wall, P flips (3,0) Gate OPEN, W enters (3,0), R enters (3,-1).
        //   * Pass 2: J(3,-1->5,-1) lands on PhaseSpikeUp on Beat 5 (ODD -> SAFE!), L merges (5,-1->5,0),
        //     P flips (6,-1) Gate CLOSED, W steps to (6,0), and R at (6,0) is CLAMPED by the newly-closed (6,-1) Gate!
        //   * Pass 3: J(6,0->8,0) lands on PhaseSpikeDown on Beat 10 (EVEN -> SAFE!), L steps to (8,1),
        //     P flips (9,1) Gate OPEN, W steps through (9,1), and R steps into (9,0) END!
        // =====================================================================
        var lvl6 = new LevelDefinition
        {
            levelIndex = 6,
            title = "L6. Beat Breaker",
            objectiveHint = "WEAVE ALL 5 MODULES! TIME YOUR [PULSE] TO SYNC SPIKE BEATS & LASER BACKSTOPS"
        };
        lvl6.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Lava, 1));      // 1x JUMP
        lvl6.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveLeft, 1));  // 1x LEFT
        lvl6.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Wait, 1));      // 1x PULSE
        lvl6.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Walk, 1));      // 1x WALK
        lvl6.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveRight, 1)); // 1x RIGHT

        // Entry & Gate (y = 0)
        lvl6.cells.Add(new TileCellData(0, 0, TileType.Start));
        lvl6.cells.Add(new TileCellData(0, -1, TileType.Lava));
        lvl6.cells.Add(new TileCellData(1, 0, TileType.Lava));
        lvl6.cells.Add(new TileCellData(2, 0, TileType.Free));
        lvl6.cells.Add(new TileCellData(2, 1, TileType.Obstacle, true));
        lvl6.cells.Add(new TileCellData(2, -1, TileType.Obstacle, true));
        lvl6.cells.Add(new TileCellData(3, 0, TileType.PhaseGateClosed, true)); // Must be opened by PULSE at (2,0)!
        lvl6.cells.Add(new TileCellData(4, 0, TileType.Obstacle, true));

        // Upper Decoy Bridge (y = +1)
        lvl6.cells.Add(new TileCellData(3, 1, TileType.Free));
        lvl6.cells.Add(new TileCellData(4, 1, TileType.Lava));
        lvl6.cells.Add(new TileCellData(5, 1, TileType.Free));
        lvl6.cells.Add(new TileCellData(6, 1, TileType.PhaseGateOpen));         // Open on even pulse parity -> traps J->R->P->W->L!
        lvl6.cells.Add(new TileCellData(7, 1, TileType.Obstacle, true));

        // Lower True Bridge (y = -1)
        lvl6.cells.Add(new TileCellData(3, -1, TileType.Free));
        lvl6.cells.Add(new TileCellData(4, -1, TileType.Lava));
        lvl6.cells.Add(new TileCellData(5, -1, TileType.PhaseSpikeUp));         // Safe on odd Beat 5!
        lvl6.cells.Add(new TileCellData(6, -1, TileType.PhaseGateClosed, true)); // Closes on Pass 2 PULSE to clamp R at (6,0)!

        // Center Re-entry & Finale (y = 0 & y = +1)
        lvl6.cells.Add(new TileCellData(5, 0, TileType.Free));
        lvl6.cells.Add(new TileCellData(6, 0, TileType.Free));
        lvl6.cells.Add(new TileCellData(7, 0, TileType.Lava));
        lvl6.cells.Add(new TileCellData(8, 0, TileType.PhaseSpikeDown));        // Safe on even Beat 10!
        lvl6.cells.Add(new TileCellData(8, 1, TileType.Free));
        lvl6.cells.Add(new TileCellData(9, 1, TileType.PhaseGateClosed, true)); // Opens on Pass 3 PULSE so W steps (8,1)->(9,1)!
        lvl6.cells.Add(new TileCellData(9, 0, TileType.End));                   // Final R steps (9,1)->(9,0) GOAL!
        levels.Add(lvl6);

        // =====================================================================
        // LEVEL 7: L7. Circuit Maze (Grand Finale)
        // A large 4-row x 11-column factory floor with multiple paths, choices, and genuine puzzle depth.
        // Provides a generous 7-node loadout (2x WALK, 2x JUMP, 1x LEFT, 1x RIGHT, 1x PULSE).
        // Players can design their own loop strategy across 9 distinct routes:
        // - Wave Weave: [JUMP] -> [LEFT] -> [JUMP] -> [RIGHT] oscillating across lanes!
        // - Laser Override: [JUMP] -> [PULSE] -> [JUMP] -> [LEFT] bypassing laser gates!
        // - Paced Stride: [WALK] -> [JUMP] -> [LEFT] -> [JUMP] -> [WALK] weaving through safe windows!
        // =====================================================================
        var lvl7 = new LevelDefinition
        {
            levelIndex = 7,
            title = "L7. Circuit Maze",
            objectiveHint = "GRAND FINALE — EXPANDED SECTOR! WIRE A 4-6 NODE LOOP: WEAVE LANES, TIME GATES, OR VAULT SPIKES"
        };
        lvl7.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Walk, 2));      // 2x WALK
        lvl7.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Lava, 2));      // 2x JUMP
        lvl7.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveLeft, 1));  // 1x LEFT
        lvl7.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveRight, 1)); // 1x RIGHT
        lvl7.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Wait, 1));      // 1x PULSE

        // 4-Row Arena Grid (z = 0 to 3, x = 0 to 10)
        lvl7.cells.Add(new TileCellData(0, 0, TileType.Free));
        lvl7.cells.Add(new TileCellData(0, 1, TileType.Start));
        lvl7.cells.Add(new TileCellData(0, 2, TileType.Free));
        lvl7.cells.Add(new TileCellData(0, 3, TileType.Free));
        lvl7.cells.Add(new TileCellData(1, 0, TileType.Obstacle, true));
        lvl7.cells.Add(new TileCellData(1, 1, TileType.Free));
        lvl7.cells.Add(new TileCellData(1, 2, TileType.Obstacle, true));
        lvl7.cells.Add(new TileCellData(1, 3, TileType.Obstacle, true));
        lvl7.cells.Add(new TileCellData(2, 0, TileType.PhaseSpikeDown));
        lvl7.cells.Add(new TileCellData(2, 1, TileType.Free));
        lvl7.cells.Add(new TileCellData(2, 2, TileType.Free));
        lvl7.cells.Add(new TileCellData(2, 3, TileType.Free));
        lvl7.cells.Add(new TileCellData(3, 0, TileType.Lava));
        lvl7.cells.Add(new TileCellData(3, 1, TileType.Obstacle, true));
        lvl7.cells.Add(new TileCellData(3, 2, TileType.Free));
        lvl7.cells.Add(new TileCellData(3, 3, TileType.Obstacle, true));
        lvl7.cells.Add(new TileCellData(4, 0, TileType.PhaseSpikeUp));
        lvl7.cells.Add(new TileCellData(4, 1, TileType.Free));
        lvl7.cells.Add(new TileCellData(4, 2, TileType.PhaseGateOpen));
        lvl7.cells.Add(new TileCellData(4, 3, TileType.Obstacle, true));
        lvl7.cells.Add(new TileCellData(5, 0, TileType.Free));
        lvl7.cells.Add(new TileCellData(5, 2, TileType.Free));
        lvl7.cells.Add(new TileCellData(5, 3, TileType.PhaseSpikeUp));
        lvl7.cells.Add(new TileCellData(6, 0, TileType.Free));
        lvl7.cells.Add(new TileCellData(6, 1, TileType.Free));
        lvl7.cells.Add(new TileCellData(6, 2, TileType.Free));
        lvl7.cells.Add(new TileCellData(6, 3, TileType.Free));
        lvl7.cells.Add(new TileCellData(7, 0, TileType.Free));
        lvl7.cells.Add(new TileCellData(7, 1, TileType.PhaseSpikeDown));
        lvl7.cells.Add(new TileCellData(7, 2, TileType.Lava));
        lvl7.cells.Add(new TileCellData(7, 3, TileType.Free));
        lvl7.cells.Add(new TileCellData(8, 0, TileType.Free));
        lvl7.cells.Add(new TileCellData(8, 1, TileType.Obstacle, true));
        lvl7.cells.Add(new TileCellData(8, 2, TileType.Free));
        lvl7.cells.Add(new TileCellData(8, 3, TileType.Obstacle, true));
        lvl7.cells.Add(new TileCellData(9, 1, TileType.Free));
        lvl7.cells.Add(new TileCellData(9, 2, TileType.Lava));
        lvl7.cells.Add(new TileCellData(9, 3, TileType.Free));
        lvl7.cells.Add(new TileCellData(10, 0, TileType.Obstacle, true));
        lvl7.cells.Add(new TileCellData(10, 1, TileType.Lava));
        lvl7.cells.Add(new TileCellData(10, 2, TileType.End));
        lvl7.cells.Add(new TileCellData(10, 3, TileType.Free));
        levels.Add(lvl7);

        // =====================================================================
        // LEVEL 8: L8. Neon Detour
        // Eight tiles with role highlights and symbols, no text (numbers are tile IDs from the design sheet, not grid indices):
        //   [7 START]-[8 WALK]-[9 GATE]-[10 ROTATE]
        //                                   |
        //                              [16 WALK]-[17 WALK]-[18 SPIKES]-[19 END]
        // Movement is grid-absolute (WALK = +X, LEFT = +Z): the robot leaves 10 for 16 with [LEFT]
        // and [WALK]s to 17. Tile 18 is a static spike (Lava) tile, so the robot must [JUMP] from 17 over it onto 19.
        // Verified loop: START -> WALK -> PULSE -> JUMP -> LEFT -> LOOP
        //   Pass 1: W (0,0)->(1,0), P opens gate 9, J vaults the open gate (1,0)->(3,0), L -> (3,1).
        //   Pass 2: W -> (4,1), P closes gate (robot is past it), J vaults spikes (4,1)->(6,1) END.
        // The previous all-WALK route (W -> P -> W -> L) now steps onto the spikes and respawns.
        // =====================================================================
        var lvl8 = new LevelDefinition
        {
            levelIndex = 8,
            title = "L8. Neon Detour",
            objectiveHint = "[PULSE] OPENS THE LASER GATE. [LEFT] SWITCHES LANES — THEN [JUMP] THE SPIKES TO REACH THE EXIT!"
        };
        lvl8.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Walk, 2));      // 2x WALK
        lvl8.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Wait, 1));      // 1x PULSE
        lvl8.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveLeft, 1));  // 1x LEFT
        lvl8.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Lava, 1));      // 1x JUMP

        lvl8.cells.Add(new TileCellData(0, 0, TileType.Start, false, TileAccent.Start));      // 7  START
        lvl8.cells.Add(new TileCellData(1, 0, TileType.Free, false, TileAccent.Walk));        // 8  WALK
        lvl8.cells.Add(new TileCellData(2, 0, TileType.PhaseGateClosed, true, TileAccent.Gate)); // 9  GATE (opened by PULSE)
        lvl8.cells.Add(new TileCellData(3, 0, TileType.Free, false, TileAccent.Rotate));      // 10 ROTATE
        lvl8.cells.Add(new TileCellData(3, 1, TileType.Free, false, TileAccent.Walk));        // 16 WALK
        lvl8.cells.Add(new TileCellData(4, 1, TileType.Free, false, TileAccent.Walk));        // 17 WALK
        lvl8.cells.Add(new TileCellData(5, 1, TileType.Lava));                                // 18 SPIKES (jump over from 17)
        lvl8.cells.Add(new TileCellData(6, 1, TileType.End, false, TileAccent.End));          // 19 END
        levels.Add(lvl8);

        // LEVEL 9: Matrix Weave combines the arena walls/gates with Rotation Run's lane switches.
        // Eight existing commands repeat over three five-column sectors. LEFT is +Z; RIGHT is -Z.
        // W -> P -> W -> L -> P -> W -> J -> R: the second pulse resets every gate for the next pass.
        // Jump lands on PhaseSpikeDown at zero-based beats 6,14,22 (safe even beats), then RIGHT leaves.
        var lvl9 = new LevelDefinition
        {
            levelIndex = 9,
            title = FinalLevelTitle,
            objectiveHint = "WEAVE THREE SECTORS! PULSE THROUGH GATES, SWITCH LANES & JUMP THE SPIKES. RESET THE GATE PHASE EACH LOOP.",
            fitCameraToBoard = true
        };
        lvl9.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Walk, 3));
        lvl9.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Wait, 2)); // Existing PULSE, not a new WAIT action.
        lvl9.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveLeft, 1));
        lvl9.unlockedNodes.Add(new NodeLoadoutItem(BlockType.Lava, 1)); // Existing JUMP.
        lvl9.unlockedNodes.Add(new NodeLoadoutItem(BlockType.MoveRight, 1));

        for (int x = 0; x <= 15; x++)
        {
            int local = x % 5;
            TileType lower = x == 0 ? TileType.Start
                : x == 15 ? TileType.End
                : local == 2 ? TileType.PhaseGateClosed
                : local == 3 ? TileType.Obstacle
                : local == 4 ? TileType.Lava : TileType.Free;
            TileAccent lowerAccent = x == 0 ? TileAccent.Start
                : x == 15 ? TileAccent.End
                : local == 2 ? TileAccent.Gate
                : local == 1 ? TileAccent.Walk
                : local == 0 ? TileAccent.Start : TileAccent.None;
            lvl9.cells.Add(new TileCellData(x, 0, lower,
                lower == TileType.Obstacle || lower == TileType.PhaseGateClosed, lowerAccent));

            TileType upper = x == 0 ? TileType.Obstacle
                : local == 0 ? TileType.PhaseSpikeDown
                : local == 1 || local == 4 ? TileType.Lava : TileType.Free;
            TileAccent upperAccent = local == 3 ? TileAccent.Jump
                : local == 2 ? TileAccent.Walk : TileAccent.None;
            lvl9.cells.Add(new TileCellData(x, 1, upper, upper == TileType.Obstacle, upperAccent));
        }
        levels.Add(lvl9);

        return levels;
    }
}

