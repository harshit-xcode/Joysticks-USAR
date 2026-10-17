// PlayerController.cs
// Controls the robot's movement across the 3D isometric tile grid.
// - WALK: Steps 1 tile forward (+X). Bumps in place if blocked.
// - JUMP: Vaults OVER the next tile (from tile X to tile X+2, skipping X+1).
//         Blocked if a tall Obstacle wall is at X+1 or if X+2 is missing/Obstacle.
//         Landing ON a LavaTile at X+2 still triggers death; jumping OVER a LavaTile at X+1 is safe!
// - LEFT / RIGHT: Sidesteps 1 tile (+Z / -Z). Bumps in place if blocked.

using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.6f;
    public float stepDelay = 0.08f;
    public float walkArcHeight = 0.18f;
    public float jumpArcHeight = 1.25f;
    public float surfaceOffset = 0.55f;

    [Header("State")]
    public PlayerState currentState = PlayerState.Idle;
    public bool isCrossingLava = false;

    private Tile currentTile;
    private readonly Vector2Int forwardDir = new Vector2Int(1, 0);
    private readonly Vector2Int leftDir = new Vector2Int(0, 1);
    private readonly Vector2Int rightDir = new Vector2Int(0, -1);

    public Tile GetCurrentTile() => currentTile;

    public Vector3 GetStandingPosition(Tile tile)
    {
        if (tile == null) return transform.position;
        return tile.transform.position + Vector3.up * surfaceOffset;
    }

    public void PlaceOnStartTile()
    {
        if (TileManager.Instance == null) return;
        StartTile start = TileManager.Instance.GetStartTile();
        if (start == null)
        {
            Debug.LogError("PlayerController: No StartTile found in the scene!");
            return;
        }

        currentTile = start;
        transform.position = GetStandingPosition(start);
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        currentState = PlayerState.Idle;
        isCrossingLava = false;
        TileManager.Instance.ResetDynamicEnvironment();
    }

    public void StopAtCurrentTile()
    {
        if (currentTile != null)
            transform.position = GetStandingPosition(currentTile);
        transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        isCrossingLava = false;
        if (currentState != PlayerState.Win && currentState != PlayerState.Dead)
            currentState = PlayerState.Idle;
    }

    // WALK node: moves 1 tile forward (+X). Does nothing (bumps) if blocked.
    public IEnumerator MoveForwardCoroutine()
    {
        yield return StepInDirectionCoroutine(forwardDir, walkArcHeight, 1.0f);
    }

    // JUMP node: vaults OVER the adjacent tile (from X to X+2, skipping X+1).
    // Blocked if a tall Obstacle is at X+1, or if X+2 has no tile or has an Obstacle.
    public IEnumerator JumpOverLavaCoroutine()
    {
        if (currentTile == null)
        {
            PlaceOnStartTile();
            if (currentTile == null) yield break;
        }

        Vector2Int midCoord = currentTile.gridPosition + forwardDir;
        Vector2Int landingCoord = currentTile.gridPosition + forwardDir * 2;

        Tile midTile = TileManager.Instance.GetTile(midCoord);
        Tile landingTile = TileManager.Instance.GetTile(landingCoord);

        // Cannot jump through an adjacent tall Obstacle wall, nor land on a missing/Obstacle tile
        bool midBlockedByWall = (midTile != null && !midTile.CanPlayerEnter(this));
        bool landingBlocked = (landingTile == null || !landingTile.CanPlayerEnter(this));

        if (midBlockedByWall || landingBlocked)
        {
            Debug.Log($"PlayerController: JUMP from {currentTile.gridPosition} to {landingCoord} blocked — bumping in place.");
            yield return BumpCoroutine(forwardDir);
            yield break;
        }

        AudioManager.Instance?.PlayJump();
        yield return StepToTileCoroutine(landingTile, jumpArcHeight, 0.82f, true);
    }

    // LEFT node: sidesteps 1 tile left (+Z). Does nothing (bumps) if blocked.
    public IEnumerator MoveLeftCoroutine()
    {
        yield return StepInDirectionCoroutine(leftDir, walkArcHeight, 1.05f);
    }

    // RIGHT node: sidesteps 1 tile right (-Z). Does nothing (bumps) if blocked.
    public IEnumerator MoveRightCoroutine()
    {
        yield return StepInDirectionCoroutine(rightDir, walkArcHeight, 1.05f);
    }

    // Legacy compatibility coroutine
    public IEnumerator ForceDashCoroutine()
    {
        yield return JumpOverLavaCoroutine();
    }

    // PULSE node (BlockType.Wait): holds position for 1 beat, emits a 3D EMP shockwave, and flips all Laser Phase Gates!
    public IEnumerator WaitStepCoroutine()
    {
        if (currentTile == null)
        {
            PlaceOnStartTile();
            if (currentTile == null) yield break;
        }

        AudioManager.Instance?.PlayNodeConnect();
        TileManager.Instance?.TriggerEmpPulse(transform.position);

        Vector3 basePos = GetStandingPosition(currentTile);
        Quaternion baseRot = Quaternion.Euler(0f, 90f, 0f);
        float duration = 1f / Mathf.Max(0.5f, moveSpeed);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = basePos + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.24f);
            transform.rotation = baseRot * Quaternion.Euler(0f, t * 360f, 0f);
            yield return null;
        }
        transform.position = basePos;
        transform.rotation = baseRot;
        yield return new WaitForSeconds(stepDelay);
    }

    private IEnumerator StepInDirectionCoroutine(Vector2Int dir, float arcHeight, float speedMultiplier)
    {
        if (currentTile == null)
        {
            PlaceOnStartTile();
            if (currentTile == null) yield break;
        }

        Tile nextTile = TileManager.Instance.GetTile(currentTile.gridPosition + dir);

        // Asymmetric obstacle behavior: if blocked or off-grid, bump in place and continue loop!
        if (nextTile == null || !nextTile.CanPlayerEnter(this))
        {
            Debug.Log($"PlayerController: Path {dir} from {currentTile.gridPosition} is blocked — node skipped.");
            yield return BumpCoroutine(dir);
            yield break;
        }

        AudioManager.Instance?.PlayWalk();
        yield return StepToTileCoroutine(nextTile, arcHeight, speedMultiplier, true);
    }

    private IEnumerator StepToTileCoroutine(Tile nextTile, float arcHeight, float speedMultiplier, bool applyStepDelay)
    {
        currentState = PlayerState.Moving;

        Vector3 startPos = transform.position;
        Vector3 endPos = GetStandingPosition(nextTile);
        Vector3 moveDelta = (endPos - startPos);
        moveDelta.y = 0f;

        Quaternion baseRot = Quaternion.Euler(0f, 90f, 0f);
        Quaternion targetRot = moveDelta.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(moveDelta.normalized, Vector3.up)
            : baseRot;

        float elapsed = 0f;
        float duration = 1f / (Mathf.Max(0.5f, moveSpeed) * speedMultiplier);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 pos = Vector3.Lerp(startPos, endPos, smoothT);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            transform.position = pos;

            float tilt = Mathf.Sin(t * Mathf.PI) * 10f;
            transform.rotation = Quaternion.Slerp(baseRot, targetRot, Mathf.Sin(t * Mathf.PI * 0.8f)) * Quaternion.Euler(tilt, 0f, 0f);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = baseRot;
        currentTile = nextTile;
        currentState = PlayerState.Idle;

        nextTile.OnPlayerEnter(this);

        if (applyStepDelay && currentState == PlayerState.Idle)
            yield return new WaitForSeconds(stepDelay);
    }

    private IEnumerator BumpCoroutine(Vector2Int dir)
    {
        AudioManager.Instance?.PlayBump();
        Vector3 basePos = currentTile != null ? GetStandingPosition(currentTile) : transform.position;
        Vector3 nudge = new Vector3(dir.x, 0f, dir.y).normalized * 0.22f;

        float duration = 0.20f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float wave = Mathf.Sin(t * Mathf.PI);
            transform.position = basePos + nudge * wave;
            yield return null;
        }
        transform.position = basePos;
        yield return new WaitForSeconds(stepDelay * 0.5f);
    }
}
