// PlayerRespawn.cs
// Handles sending the player back to the Start tile.
// Called by LavaTile when the player steps on lava without a LavaBlock.
// The active command loop waits for respawn and retries automatically.

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
    // Restarts the current loop iteration after sending the robot back to Start.
    public void Respawn()
    {
        // Prevent multiple respawns at the same time
        if (playerController.currentState == PlayerState.Dead) return;

        playerController.currentState = PlayerState.Dead;

        // Ask the active loop to restart its body after respawn
        CodeExecutor executor = FindFirstObjectByType<CodeExecutor>();
        if (executor != null)
        {
            executor.RestartAfterRespawn();
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
