using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Loops
{
    [DisallowMultipleComponent]
    public sealed class RobotProgramRunner : MonoBehaviour
    {
        public LevelBoard board;
        public Transform robot;
        [Min(.05f)] public float commandDuration = .35f;
        public float jumpHeight = .9f;

        public event Action Changed;
        public RobotSimulation Simulation { get; private set; }
        public ProgramBlock ActiveBlock { get; private set; }
        public bool IsRunning { get; private set; }
        public string Status { get; private set; } = "Initializing...";
        Dictionary<Vector2Int, LevelTile> tiles;
        Coroutine routine;

        void Start() { ResetLevel(); }
        void OnDisable() { StopProgram(); }

        public void ResetLevel()
        {
            Cancel();
            try
            {
                if (board == null || robot == null) throw new ArgumentException("Assign the board and robot on RobotProgramRunner.");
                tiles = board.ReadTiles();
                var kinds = new Dictionary<Vector2Int, TileKind>();
                var blocked = new List<Vector2Int>();
                foreach (var entry in tiles)
                {
                    kinds.Add(entry.Key, entry.Value.kind);
                    if (entry.Value.startsBlocked) blocked.Add(entry.Key);
                }
                Simulation = new RobotSimulation(kinds, blocked, board.spawn, board.spawnFacing);
                SyncVisuals();
                SetStatus(Simulation.Message);
            }
            catch (ArgumentException error) { Simulation = null; SetStatus("Level setup: " + error.Message); }
        }

        public void Run(IReadOnlyList<ProgramBlock> blocks)
        {
            if (IsRunning) return;
            List<ProgramBlock> compiled;
            try { compiled = ProgramCompiler.Compile(blocks); }
            catch (ArgumentException error) { SetStatus(error.Message); return; }
            ResetLevel();
            if (Simulation == null) return;
            Simulation.Begin();
            IsRunning = true;
            routine = StartCoroutine(Execute(compiled));
        }

        public void StopProgram()
        {
            if (!IsRunning) return;
            Cancel();
            Simulation?.Stop();
            SyncVisuals();
            SetStatus(Simulation?.Message ?? "Stopped.");
        }

        IEnumerator Execute(List<ProgramBlock> program)
        {
            // Ensure coroutine handle is assigned before it can finish on an invalid first command.
            yield return null;
            foreach (var block in program)
            {
                ActiveBlock = block;
                SetStatus($"Running: {block.kind}");
                var from = robot.position;
                var rotation = robot.rotation;
                if (!Simulation.Step(block.kind)) break;
                var to = board.RobotPosition(Simulation.Position);
                var targetRotation = board.transform.rotation * Quaternion.Euler(0, Simulation.Facing * 90, 0);
                float duration = Mathf.Max(.05f, commandDuration);
                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    float progress = Mathf.Clamp01(t / duration);
                    robot.position = Vector3.Lerp(from, to, progress);
                    if (block.kind == CommandKind.Jump)
                        robot.position += board.transform.up * (Mathf.Sin(progress * Mathf.PI) * jumpHeight);
                    robot.rotation = Quaternion.Slerp(rotation, targetRotation, progress);
                    yield return null;
                }
                SyncVisuals();
                if (Simulation.State != RobotState.Running) break;
            }
            Simulation.Finish();
            IsRunning = false;
            ActiveBlock = null;
            routine = null;
            SetStatus(Simulation.Message);
        }

        void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            IsRunning = false;
            ActiveBlock = null;
        }

        void SyncVisuals()
        {
            if (Simulation == null || board == null || robot == null) return;
            robot.position = board.RobotPosition(Simulation.Position);
            robot.rotation = board.transform.rotation * Quaternion.Euler(0, Simulation.Facing * 90, 0);
            foreach (var entry in tiles) entry.Value.ShowObstacle(Simulation.IsBlocked(entry.Key));
        }

        void SetStatus(string message) { Status = message; Changed?.Invoke(); }
    }
}
