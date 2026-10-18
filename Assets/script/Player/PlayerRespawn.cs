// PlayerRespawn.cs
// Handles sending the player back to the Start tile.
// Called by LavaTile when the player steps on lava without a LavaBlock.
// A hazard ends the active program; respawn returns the robot to Start without retrying.

using System.Collections;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    public float respawnDelay = 0.5f;   // Short pause before respawning (for visual feedback)

    private PlayerController playerController;
    private Coroutine respawnRoutine;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    // Triggered when the player hits a lava tile without a LavaBlock.
    // Captures the failing tile before respawn changes position and resets the beat clock.
    public void Respawn()
    {
        // Prevent multiple respawns at the same time
        if (playerController.currentState == PlayerState.Dead) return;

        playerController.currentState = PlayerState.Dead;

        // Report the current command's failure; execution stops at the command boundary.
        CodeExecutor executor = FindFirstObjectByType<CodeExecutor>();
        if (executor != null)
        {
            executor.ReportHazardFailure(playerController.GetCurrentTile());
        }

        respawnRoutine = StartCoroutine(DoRespawn());
    }

    public void CancelPendingRespawn()
    {
        if (respawnRoutine != null) StopCoroutine(respawnRoutine);
        respawnRoutine = null;
    }

    void OnDisable() { CancelPendingRespawn(); }

    // Short delay then teleport back to start.
    private IEnumerator DoRespawn()
    {
        Debug.Log("Respawning player...");

        yield return new WaitForSeconds(respawnDelay);

        // Teleport back to start tile
        respawnRoutine = null;
        playerController.PlaceOnStartTile();

        Debug.Log("Player respawned at Start tile.");
    }
}
