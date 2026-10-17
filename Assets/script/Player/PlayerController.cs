// PlayerController.cs
// Controls the robot's movement across the isometric tile grid.
// Movement is tile-by-tile, with a smooth lerp animation between tiles.
// The CodeExecutor calls MoveForwardCoroutine() to move the robot one tile.

using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;           // Speed of movement between tiles (units/sec)
    public float stepDelay = 0.1f;        // Small pause after landing on a tile

    [Header("State")]
    public PlayerState currentState = PlayerState.Idle;

    // Set to true by LavaBlock before crossing lava,
    // so LavaTile knows not to trigger a respawn.
    public bool isCrossingLava = false;

    // The tile the player is currently standing on
    private Tile currentTile;

    // Direction the player is moving along the row (always +1 for forward in Level 1)
    private Vector2Int moveDirection = new Vector2Int(1, 0);

    // Place the player on the start tile at the beginning of the level.
    public void PlaceOnStartTile()
    {
        StartTile start = TileManager.Instance.GetStartTile();
        if (start == null)
        {
            Debug.LogError("PlayerController: No StartTile found in the scene!");
            return;
        }

        currentTile = start;
        transform.position = start.transform.position;
        currentState = PlayerState.Idle;
        isCrossingLava = false;

        Debug.Log("Player placed on Start tile.");
    }

    // Cancel an interrupted animation at the last fully reached tile.
    public void StopAtCurrentTile()
    {
        if (currentTile != null) transform.position = currentTile.transform.position;
        isCrossingLava = false;
        if (currentState != PlayerState.Win && currentState != PlayerState.Dead)
            currentState = PlayerState.Idle;
    }

    // Returns the tile the player is currently on.
    public Tile GetCurrentTile()
    {
        return currentTile;
    }

    // Moves the player one tile forward (in moveDirection).
    // Called by WalkBlock during code execution.
    // Smoothly lerps the player to the next tile's world position.
    public IEnumerator MoveForwardCoroutine()
    {
        Tile nextTile = TileManager.Instance.GetTile(currentTile.gridPosition + moveDirection);

        if (nextTile == null)
        {
            Debug.LogWarning("PlayerController: No tile ahead — player cannot move forward.");
            yield break;
        }

        currentState = PlayerState.Moving;

        Vector3 startPos = transform.position;
        Vector3 endPos = nextTile.transform.position;
        float elapsed = 0f;
        float duration = 1f / moveSpeed;

        // Smoothly move to the next tile
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        transform.position = endPos;
        currentTile = nextTile;
        currentState = PlayerState.Idle;

        // Notify the tile that the player has arrived
        nextTile.OnPlayerEnter(this);

        yield return new WaitForSeconds(stepDelay);
    }

    // Same as MoveForwardCoroutine but wraps lava crossing safely.
    // Called by LavaBlock — sets isCrossingLava = true before moving,
    // then resets it after landing.
    public IEnumerator JumpOverLavaCoroutine()
    {
        isCrossingLava = true;

        yield return MoveForwardCoroutine();

        isCrossingLava = false;
    }
}
