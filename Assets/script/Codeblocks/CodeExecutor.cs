using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CodeExecutor : MonoBehaviour
{
    public PlayerController player;
    [Min(.02f), Tooltip("Brief pause between repeats. Commands continue until the goal is reached.")]
    public float loopDelay = .15f;

    bool isRunning;
    bool restartAfterRespawn;
    Coroutine runCoroutine;
    ProgramLoopCursor cursor;

    public void Run(List<CodeBlock> sequence)
    {
        if (isRunning) return;
        if (player == null) { Debug.LogError("Assign the robot player in CodeExecutor.", this); return; }
        if (LevelComplete()) { Debug.Log("Level already complete. Restart the level to play again."); return; }
        if (player.currentState == PlayerState.Dead) return;
        var body = new List<CodeBlock>();
        if (sequence != null)
            foreach (var block in sequence)
                if (block != null && (block.blockType == BlockType.Walk || block.blockType == BlockType.Lava))
                    body.Add(block);
        if (body.Count == 0) { Debug.LogWarning("Add at least one Walk or Jump command.", this); return; }

        cursor = new ProgramLoopCursor(body.Count);
        restartAfterRespawn = false;
        isRunning = true;
        runCoroutine = StartCoroutine(ExecuteLoop(body));
    }

    IEnumerator ExecuteLoop(List<CodeBlock> body)
    {
        // Establish the coroutine handle before a command can reach the goal and cancel it.
        yield return null;
        Debug.Log("Program started. Repeating commands until the End tile is reached.");
        while (isRunning && player != null && !LevelComplete() && cursor.TryNext(out int index))
        {
            var block = body[index];
            if (block == null) { StopExecution(); yield break; }
            // Nested enumerators belong to this run, so cancelling also stops their movement.
            yield return block.Execute(player);
            if (!isRunning || player == null || LevelComplete()) break;
            if (restartAfterRespawn)
            {
                while (isRunning && player != null && player.currentState == PlayerState.Dead)
                    yield return null;
                if (!isRunning || player == null || LevelComplete()) break;
                restartAfterRespawn = false;
                cursor.RestartIteration();
                Debug.Log("Robot respawned. Retrying the same command loop from the beginning.");
            }
            // Always yield even if a command cannot move, keeping Unity responsive.
            if (index == body.Count - 1) yield return new WaitForSeconds(Mathf.Max(.02f, loopDelay));
            else yield return null;
        }
        cursor?.Complete();
        isRunning = false;
        runCoroutine = null;
    }

    bool LevelComplete()
    {
        return (player != null && player.currentState == PlayerState.Win) ||
            (LevelManager.Instance != null && LevelManager.Instance.IsLevelComplete);
    }

    // Lava respawns are retries of the same run, not the end of the program.
    public void RestartAfterRespawn()
    {
        if (isRunning) restartAfterRespawn = true;
    }

    public void CompleteLevel()
    {
        StopExecution();
        Debug.Log("Goal reached. Command loop stopped; level complete.");
    }

    // Explicit reset, scene changes, and disabling the component still cancel cleanly.
    public void StopExecution()
    {
        isRunning = false;
        cursor?.Complete();
        restartAfterRespawn = false;
        if (runCoroutine != null) StopCoroutine(runCoroutine);
        runCoroutine = null;
        if (player != null) player.StopAtCurrentTile();
    }

    void OnDisable() { StopExecution(); }
    public bool IsRunning() { return isRunning; }
}
