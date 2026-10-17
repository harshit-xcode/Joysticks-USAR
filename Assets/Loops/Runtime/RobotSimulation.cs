using System;
using System.Collections.Generic;
using UnityEngine;

namespace Loops
{
    public enum RobotState { Ready, Running, Won, Failed, Finished, Stopped }

    // Deterministic rules, independent of animation, colliders, or visual models.
    public sealed class RobotSimulation
    {
        static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        readonly Dictionary<Vector2Int, TileKind> tiles = new Dictionary<Vector2Int, TileKind>();
        readonly HashSet<Vector2Int> initialObstacles = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> obstacles = new HashSet<Vector2Int>();
        readonly Vector2Int spawn;
        readonly int spawnFacing;

        public Vector2Int Position { get; private set; }
        public int Facing { get; private set; }
        public RobotState State { get; private set; }
        public string Message { get; private set; }
        public Vector2Int Ahead => Position + Directions[Facing];
        public bool IsBlocked(Vector2Int cell) => obstacles.Contains(cell);

        public RobotSimulation(Dictionary<Vector2Int, TileKind> tiles, IEnumerable<Vector2Int> blocked,
            Vector2Int spawn, int facing)
        {
            if (tiles == null || blocked == null) throw new ArgumentNullException();
            foreach (var entry in tiles) this.tiles.Add(entry.Key, entry.Value);
            foreach (var cell in blocked) initialObstacles.Add(cell);
            if (!this.tiles.TryGetValue(spawn, out var start) || start != TileKind.Safe || initialObstacles.Contains(spawn))
                throw new ArgumentException("Spawn must be an unblocked safe tile.");
            foreach (var cell in initialObstacles)
                if (!this.tiles.TryGetValue(cell, out var tile) || tile != TileKind.Safe)
                    throw new ArgumentException("Obstacles must be on safe tiles.");
            this.spawn = spawn;
            spawnFacing = ((facing % 4) + 4) % 4;
            Reset();
        }

        public void Reset()
        {
            obstacles.Clear();
            obstacles.UnionWith(initialObstacles);
            Position = spawn;
            Facing = spawnFacing;
            State = RobotState.Ready;
            Message = "Ready. Build a program and press Run.";
        }

        public void Begin() { Reset(); State = RobotState.Running; Message = "Running..."; }
        public void Stop() { if (State == RobotState.Running) { State = RobotState.Stopped; Message = "Stopped. Run starts again from spawn."; } }
        public void Finish()
        {
            if (State != RobotState.Running) return;
            State = RobotState.Finished;
            Message = "Program finished before the goal. Edit your commands and try again.";
        }

        public bool Step(CommandKind command)
        {
            if (State != RobotState.Running) return false;
            switch (command)
            {
                case CommandKind.TurnLeft: Facing = (Facing + 3) % 4; return true;
                case CommandKind.TurnRight: Facing = (Facing + 1) % 4; return true;
                case CommandKind.ClearObstacle:
                    if (!obstacles.Remove(Ahead)) return Fail("There is no obstacle directly ahead to clear.");
                    return true;
                case CommandKind.Move: return MoveTo(Ahead);
                case CommandKind.Jump:
                    if (!tiles.TryGetValue(Ahead, out var gap) || gap != TileKind.Lava)
                        return Fail("Jump needs one lava tile directly ahead.");
                    return MoveTo(Position + Directions[Facing] * 2);
                default: return Fail("Invalid executable command.");
            }
        }

        bool MoveTo(Vector2Int target)
        {
            if (!tiles.TryGetValue(target, out var tile)) return Fail("No tile ahead. The robot cannot leave the path.");
            if (tile == TileKind.Lava) return Fail("Lava ahead! Use Jump from the safe tile before it.");
            if (obstacles.Contains(target)) return Fail("Path blocked. Clear the obstacle from the adjacent tile.");
            Position = target;
            if (tile == TileKind.Goal) { State = RobotState.Won; Message = "Goal reached! Loop complete."; }
            return true;
        }

        bool Fail(string reason) { State = RobotState.Failed; Message = reason; return false; }
    }
}
