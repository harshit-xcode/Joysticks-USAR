using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeExecutor : MonoBehaviour
{
    public PlayerController player;
    [Min(.02f), Tooltip("Brief pause between loop passes. Commands continue until the goal is reached.")]
    public float loopDelay = .18f;

    public event Action<int, BlockType> OnStepStarted;
    public event Action<int> OnLoopPassStarted;
    public event Action OnExecutionEnded;
    public event Action<string> OnStatusMessage;
    public event Action<ExecutionFailure> OnExecutionFailed;
    public event Action OnFailureCleared;

    LoopGhostTrail ghostTrails;
    bool isRunning;
    ExecutionFailure pendingFailure;
    int currentStepIndex = -1;
    BlockType currentStepType;
    Vector2Int commandStartGrid;
    Coroutine runCoroutine;
    ProgramLoopCursor cursor;
    int currentPass;

    public int CurrentPass => currentPass;
    public ExecutionFailure LastFailure { get; private set; }

    public void Run(List<CodeBlock> sequence)
    {
        if (isRunning) return;
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogError("Assign the robot player in CodeExecutor.", this);
                return;
            }
        }
        if (LevelComplete())
        {
            Debug.Log("Level already complete. Restart the level to play again.");
            return;
        }
        if (player.currentState == PlayerState.Dead) return;

        var body = new List<CodeBlock>();
        if (sequence != null)
        {
            foreach (var block in sequence)
            {
                if (block != null && ProgramSequence.IsEditableCommand(block.blockType))
                    body.Add(block);
            }
        }

        if (body.Count == 0)
        {
            OnStatusMessage?.Invoke("CONNECT START → NODES → END FIRST!");
            Debug.LogWarning("Connect at least one node between START and END.", this);
            return;
        }

        if (ghostTrails == null)
            ghostTrails = GetComponent<LoopGhostTrail>() ?? gameObject.AddComponent<LoopGhostTrail>();
        ghostTrails.BeginRun(player);
        cursor = new ProgramLoopCursor(body.Count);
        ClearFailure();
        pendingFailure = null;
        currentStepIndex = -1;
        isRunning = true;
        currentPass = 1;
        TileManager.Instance?.ResetDynamicEnvironment();
        runCoroutine = StartCoroutine(ExecuteLoop(body));
    }

    IEnumerator ExecuteLoop(List<CodeBlock> body)
    {
        yield return null;
        OnLoopPassStarted?.Invoke(currentPass);
        OnStatusMessage?.Invoke($"EXECUTING LOOP — PASS #{currentPass}");

        Vector2Int passStartGrid = player.GetCurrentTile() != null ? player.GetCurrentTile().gridPosition : Vector2Int.zero;
        int stationaryPasses = 0;

        while (isRunning && player != null && !LevelComplete() && cursor.TryNext(out int index))
        {
            if (index == 0)
            {
                passStartGrid = player.GetCurrentTile() != null ? player.GetCurrentTile().gridPosition : Vector2Int.zero;
            }

            var block = body[index];
            if (block == null) { StopExecution(); yield break; }

            currentStepIndex = index;
            currentStepType = block.blockType;
            commandStartGrid = player.GetCurrentTile() != null ? player.GetCurrentTile().gridPosition : Vector2Int.zero;
            OnStepStarted?.Invoke(index, block.blockType);
            OnStatusMessage?.Invoke($"PASS #{currentPass} // NODE {index + 1}: {CommandLabels.For(block.blockType)}");

            yield return block.Execute(player);

            if (!isRunning || player == null || LevelComplete()) break;

            if (pendingFailure == null)
            {
                TileManager.Instance?.CompleteBeat(player);
            }

            if (pendingFailure != null)
            {
                // End this attempt once. Respawn may finish independently; never restart the program.
                ghostTrails?.FinishPass();
                var failure = pendingFailure;
                LastFailure = failure;
                pendingFailure = null;
                cursor.Complete();
                isRunning = false;
                runCoroutine = null;
                currentStepIndex = -1;
                OnExecutionEnded?.Invoke();
                OnExecutionFailed?.Invoke(failure);
                OnStatusMessage?.Invoke("Recheck your Circuit");
                yield break;
            }

            if (index == body.Count - 1)
            {
                Vector2Int passEndGrid = player.GetCurrentTile() != null ? player.GetCurrentTile().gridPosition : Vector2Int.zero;
                if (passEndGrid == passStartGrid)
                {
                    stationaryPasses++;
                    if (stationaryPasses >= 2)
                    {
                        OnStatusMessage?.Invoke("LOOP BLOCKED BY OBSTACLES — REWIRE NODES!");
                    }
                }
                else
                {
                    stationaryPasses = 0;
                }

                currentPass++;
                ghostTrails?.BeginPass(currentPass);
                OnLoopPassStarted?.Invoke(currentPass);
                yield return new WaitForSeconds(Mathf.Max(.02f, loopDelay));
            }
            else
            {
                yield return null;
            }
        }

        ghostTrails?.FinishPass();
        cursor?.Complete();
        isRunning = false;
        runCoroutine = null;
        OnExecutionEnded?.Invoke();
    }

    bool LevelComplete()
    {
        return (player != null && player.currentState == PlayerState.Win) ||
            (LevelManager.Instance != null && LevelManager.Instance.IsLevelComplete);
    }

    public void RestartAfterRespawn()
    {
        // Compatibility for older callers: a respawn now ends the attempt rather than retrying it.
        ReportHazardFailure(player != null ? player.GetCurrentTile() : null);
    }

    public void ReportHazardFailure(Tile tile)
    {
        if (!isRunning || pendingFailure != null || currentStepIndex < 0) return;
        string reason = tile is PhaseSpikeTile
            ? (tile.gridPosition == commandStartGrid ? "stayed on raised spikes" : "landed on raised spikes")
            : tile is PhaseGateTile ? "was caught by a closing laser gate"
            : tile is LavaTile ? "landed on red spikes (lava)" : "hit a hazard";
        pendingFailure = new ExecutionFailure(currentPass, currentStepIndex, currentStepType,
            tile != null ? tile.gridPosition : Vector2Int.zero,
            (TileManager.Instance != null ? TileManager.Instance.CurrentBeat : 0) + 1, reason);
    }

    public void ClearFailure()
    {
        if (LastFailure == null) return;
        LastFailure = null;
        OnFailureCleared?.Invoke();
    }

    public void CompleteLevel()
    {
        ghostTrails?.FinishPass();
        StopExecution(false);
        OnStatusMessage?.Invoke("GOAL REACHED // TEST DEPLOYMENT COMPLETE!");
        Debug.Log("Goal reached. Command loop stopped; level complete.");
    }

    public void StopExecution() { StopExecution(true); }

    void StopExecution(bool clearGhostTrails)
    {
        if (clearGhostTrails) ghostTrails?.ClearTrails();
        bool wasRunning = isRunning;
        isRunning = false;
        cursor?.Complete();
        pendingFailure = null;
        currentStepIndex = -1;
        ClearFailure();
        if (runCoroutine != null) StopCoroutine(runCoroutine);
        runCoroutine = null;
        if (player != null) player.StopAtCurrentTile();
        if (wasRunning) OnExecutionEnded?.Invoke();
    }

    void OnDisable() { StopExecution(); }
    public bool IsRunning() { return isRunning; }
}
