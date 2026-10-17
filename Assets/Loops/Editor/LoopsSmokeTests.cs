using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Loops.Editor
{
    public static class LoopsSmokeTests
    {
        static int checks;
        [MenuItem("Loops/Run Rules Smoke Tests")]
        public static void Run()
        {
            checks = 0;
            var repeat = new ProgramBlock(CommandKind.Repeat) { repetitions = 3 };
            repeat.children.Add(new ProgramBlock(CommandKind.Move));
            repeat.children.Add(new ProgramBlock(CommandKind.Jump));
            repeat.children.Add(new ProgramBlock(CommandKind.ClearObstacle));
            repeat.children.Add(new ProgramBlock(CommandKind.Move));
            var program = ProgramCompiler.Compile(new[] { repeat });
            Check(program.Count == 12, "Repeat expands commands");
            var cells = new Dictionary<Vector2Int, TileKind>();
            var obstacles = new List<Vector2Int>();
            for (int z = 0; z <= 12; z++)
            {
                var cell = new Vector2Int(0, z);
                cells.Add(cell, z == 12 ? TileKind.Goal : z % 4 == 2 ? TileKind.Lava : TileKind.Safe);
                if (z > 0 && z % 4 == 0) obstacles.Add(cell);
            }
            // Goal is never blocked: route's last clear is for z=12, so use a safe final landing
            // and one extra move to the goal at z=13.
            cells[new Vector2Int(0, 12)] = TileKind.Safe;
            cells.Add(new Vector2Int(0, 13), TileKind.Goal);
            var sim = new RobotSimulation(cells, obstacles, Vector2Int.zero, 0);
            sim.Begin();
            foreach (var block in program) Check(sim.Step(block.kind), "Repeated move/jump/clear succeeds");
            Check(sim.Step(CommandKind.Move) && sim.State == RobotState.Won, "Goal reached");
            Check(!sim.Step(CommandKind.Move), "Won simulation cannot execute");
            sim.Reset();
            Check(sim.Position == Vector2Int.zero && sim.IsBlocked(new Vector2Int(0, 4)), "Reset restores spawn and obstacle");
            sim.Begin(); sim.Step(CommandKind.Move);
            Check(!sim.Step(CommandKind.Move) && sim.State == RobotState.Failed, "Walking into lava fails");
            sim.Begin();
            Check(!sim.Step(CommandKind.Jump), "Jump requires lava ahead");
            sim.Begin(); sim.Step(CommandKind.TurnLeft);
            Check(!sim.Step(CommandKind.Move) && sim.Position == Vector2Int.zero, "Off-grid move fails without moving");
            sim.Begin();
            Check(!sim.Step(CommandKind.ClearObstacle), "Clearing empty tile fails");
            sim.Begin(); sim.Step(CommandKind.Move); sim.Step(CommandKind.Jump);
            Check(!sim.Step(CommandKind.Move), "Cannot move into obstacle");
            sim.Begin(); sim.Stop();
            Check(!sim.Step(CommandKind.Move) && sim.State == RobotState.Stopped, "Stopped simulation does not advance");
            sim.Begin(); sim.Finish();
            Check(sim.State == RobotState.Finished, "Exhausted program without goal reports finished");
            var inner = new ProgramBlock(CommandKind.Repeat) { repetitions = 2 };
            inner.children.Add(new ProgramBlock(CommandKind.TurnLeft));
            var outer = new ProgramBlock(CommandKind.Repeat) { repetitions = 3 };
            outer.children.Add(inner);
            Check(ProgramCompiler.Compile(new[] { outer }).Count == 6, "Nested repeats expand");
            Throws(() => ProgramCompiler.Compile(new ProgramBlock[0]), "Empty program rejected");
            Throws(() => ProgramCompiler.Compile(new[] { new ProgramBlock(CommandKind.Repeat) }), "Empty loop rejected");
            inner.repetitions = 0;
            Throws(() => ProgramCompiler.Compile(new[] { inner }), "Zero repeat rejected");
            inner.repetitions = 21;
            Throws(() => ProgramCompiler.Compile(new[] { inner }), "Excessive repeat rejected");
            inner.repetitions = 20; outer.repetitions = 20;
            var huge = new ProgramBlock(CommandKind.Repeat) { repetitions = 2 };
            huge.children.Add(outer);
            Throws(() => ProgramCompiler.Compile(new[] { huge }), "Instruction budget enforced");
            inner.children.Add(inner);
            Throws(() => ProgramCompiler.Compile(new[] { inner }), "Circular loop rejected");
            var deep = new ProgramBlock(CommandKind.Move);
            for (int i = 0; i < 9; i++)
            {
                var parent = new ProgramBlock(CommandKind.Repeat) { repetitions = 1 };
                parent.children.Add(deep); deep = parent;
            }
            Throws(() => ProgramCompiler.Compile(new[] { deep }), "Nesting budget enforced");
            var jumpMap = new Dictionary<Vector2Int, TileKind>
            {
                [Vector2Int.zero] = TileKind.Safe, [Vector2Int.up] = TileKind.Lava,
                [Vector2Int.up * 2] = TileKind.Lava
            };
            var badJump = new RobotSimulation(jumpMap, Array.Empty<Vector2Int>(), Vector2Int.zero, 0);
            badJump.Begin();
            Check(!badJump.Step(CommandKind.Jump) && badJump.Position == Vector2Int.zero, "Lava landing rejected");
            jumpMap.Remove(Vector2Int.up * 2);
            badJump = new RobotSimulation(jumpMap, Array.Empty<Vector2Int>(), Vector2Int.zero, 0);
            badJump.Begin();
            Check(!badJump.Step(CommandKind.Jump), "Missing landing rejected");
            Debug.Log($"LOOPS_TESTS_PASSED: {checks} checks");
        }

        // Command-line entry point creates the shared scene and validates its real tile setup.
        public static void BuildAndTest()
        {
            Run();
            LoopsDemoBuilder.CreateDemo();
            var board = UnityEngine.Object.FindFirstObjectByType<LevelBoard>();
            var tiles = board.ReadTiles();
            var map = new Dictionary<Vector2Int, TileKind>();
            var obstacles = new List<Vector2Int>();
            foreach (var entry in tiles)
            {
                map.Add(entry.Key, entry.Value.kind);
                if (entry.Value.startsBlocked) obstacles.Add(entry.Key);
            }
            var sim = new RobotSimulation(map, obstacles, board.spawn, board.spawnFacing);
            sim.Begin();
            // Demo goal has no obstacle. First two segments clear logs; final segment lands on goal.
            var loop = new ProgramBlock(CommandKind.Repeat) { repetitions = 2 };
            loop.children.Add(new ProgramBlock(CommandKind.Move));
            loop.children.Add(new ProgramBlock(CommandKind.Jump));
            loop.children.Add(new ProgramBlock(CommandKind.ClearObstacle));
            loop.children.Add(new ProgramBlock(CommandKind.Move));
            var solution = new List<ProgramBlock> { loop, new ProgramBlock(CommandKind.Move),
                new ProgramBlock(CommandKind.Jump), new ProgramBlock(CommandKind.Move) };
            foreach (var block in ProgramCompiler.Compile(solution)) Check(sim.Step(block.kind), "Actual demo solution step");
            Check(sim.State == RobotState.Won, "Actual shared demo is solvable");
            Debug.Log("LOOPS_DEMO_VERIFIED");
        }

        static void Check(bool condition, string description)
        {
            if (!condition) throw new Exception("LOOPS_TEST_FAILED: " + description);
            checks++;
        }
        static void Throws(Action action, string description)
        {
            bool threw = false;
            try { action(); } catch (ArgumentException) { threw = true; }
            Check(threw, description);
        }
    }
}
