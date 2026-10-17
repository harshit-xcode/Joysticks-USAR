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

    bool isRunning;
    bool restartAfterRespawn;
    Coroutine runCoroutine;
    ProgramLoopCursor cursor;
    int currentPass;

    public int CurrentPass => currentPass;

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

        cursor = new ProgramLoopCursor(body.Count);
        restartAfterRespawn = false;
        isRunning = true;
        currentPass = 1;
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

            OnStepStarted?.Invoke(index, block.blockType);
            OnStatusMessage?.Invoke($"PASS #{currentPass} // NODE {index + 1}: {CommandLabels.For(block.blockType)}");

            yield return block.Execute(player);

            if (!isRunning || player == null || LevelComplete()) break;

            if (restartAfterRespawn)
            {
                OnStatusMessage?.Invoke("HAZARD HIT — RESPAWNING AT START...");
                while (isRunning && player != null && player.currentState == PlayerState.Dead)
                    yield return null;
                if (!isRunning || player == null || LevelComplete()) break;

                restartAfterRespawn = false;
                cursor.RestartIteration();
                currentPass = 1;
                stationaryPasses = 0;
                OnLoopPassStarted?.Invoke(currentPass);
                OnStatusMessage?.Invoke("RESPAWNED — RETRYING LOOP PASS #1");
                continue;
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
                OnLoopPassStarted?.Invoke(currentPass);
                yield return new WaitForSeconds(Mathf.Max(.02f, loopDelay));
            }
            else
            {
                yield return null;
            }
        }

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
        if (isRunning) restartAfterRespawn = true;
    }

    public void CompleteLevel()
    {
        StopExecution();
        OnStatusMessage?.Invoke("GOAL REACHED // TEST DEPLOYMENT COMPLETE!");
        Debug.Log("Goal reached. Command loop stopped; level complete.");
    }

    public void StopExecution()
    {
        bool wasRunning = isRunning;
        isRunning = false;
        cursor?.Complete();
        restartAfterRespawn = false;
        if (runCoroutine != null) StopCoroutine(runCoroutine);
        runCoroutine = null;
        if (player != null) player.StopAtCurrentTile();
        if (wasRunning) OnExecutionEnded?.Invoke();
    }

    void OnDisable() { StopExecution(); }
    public bool IsRunning() { return isRunning; }
}
