// TileManager.cs
// Manages the 3D isometric tile grid AND the Dynamic Factory Clock (CurrentBeat + IsGateFlipped)
// for Hydraulic Phase Spikes and Laser Phase Gates.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    [Header("Grid Settings")]
    [Tooltip("World-space distance between adjacent grid cells.")]
    public float cellSpacing = 2.0f;

    [Header("Dynamic Environment State (Read-Only Telemetry)")]
    [SerializeField] private int currentBeat = 0;
    [SerializeField] private bool isGateFlipped = false;

    public int CurrentBeat => currentBeat;
    public bool IsGateFlipped => isGateFlipped;

    private readonly Dictionary<Vector2Int, Tile> tileGrid = new Dictionary<Vector2Int, Tile>();
    private readonly List<PhaseSpikeTile> phaseSpikeTiles = new List<PhaseSpikeTile>();
    private readonly List<PhaseGateTile> phaseGateTiles = new List<PhaseGateTile>();
    private StartTile startTile;
    private EndTile endTile;

    public IReadOnlyDictionary<Vector2Int, Tile> AllTiles => tileGrid;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RegisterAllTiles();
    }

    // Scans all active Tile components in the scene and indexes them by gridPosition.
    public void RegisterAllTiles()
    {
        tileGrid.Clear();
        phaseSpikeTiles.Clear();
        phaseGateTiles.Clear();
        startTile = null;
        endTile = null;

        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (Tile tile in allTiles)
        {
            if (tile == null || !tile.gameObject.activeInHierarchy) continue;

            tileGrid[tile.gridPosition] = tile;

            if (tile is StartTile st) startTile = st;
            else if (tile is EndTile et) endTile = et;
            else if (tile is PhaseSpikeTile pst) phaseSpikeTiles.Add(pst);
            else if (tile is PhaseGateTile pgt) phaseGateTiles.Add(pgt);
        }

        ResetDynamicEnvironment();
        Debug.Log($"TileManager: Registered {tileGrid.Count} tiles ({phaseSpikeTiles.Count} phase spikes, {phaseGateTiles.Count} phase gates).");
    }

    public void ResetDynamicEnvironment()
    {
        currentBeat = 0;
        isGateFlipped = false;

        foreach (var pst in phaseSpikeTiles)
            if (pst != null) pst.SyncVisualImmediate();
        foreach (var pgt in phaseGateTiles)
            if (pgt != null) pgt.SyncVisualImmediate();
    }

    // Called by [PULSE] (WaitStepCoroutine): toggles all Laser Phase Gates and spawns a 3D EMP shockwave ring!
    public void TriggerEmpPulse(Vector3 originWorldPos)
    {
        isGateFlipped = !isGateFlipped;
        Debug.Log($"TileManager: [PULSE] executed on beat {currentBeat} -> Laser Phase Gates flipped (isGateFlipped={isGateFlipped}).");

        if (Application.isPlaying)
        {
            StartCoroutine(SpawnEmpShockwaveRingCoroutine(originWorldPos));
        }
    }

    // Called by CodeExecutor at the end of every executed node (1 beat):
    // 1. Checks if the tile the robot is currently standing on is lethal on `currentBeat`
    //    (catches lingering on a PhaseSpike via BUMP/PULSE or closing a PhaseGate on oneself!).
    // 2. Increments `currentBeat++` so PhaseSpikes alternate for the next beat!
    public void CompleteBeat(PlayerController player)
    {
        if (player != null && player.currentState != PlayerState.Dead && player.currentState != PlayerState.Win)
        {
            Tile cur = player.GetCurrentTile();
            if (cur is PhaseSpikeTile pst && pst.IsLethalOnCurrentBeat())
            {
                pst.OnPlayerEnter(player);
                return;
            }
            if (cur is PhaseGateTile pgt && pgt.IsClosedRightNow())
            {
                pgt.OnPlayerEnter(player);
                return;
            }
        }

        currentBeat++;
    }

    private IEnumerator SpawnEmpShockwaveRingCoroutine(Vector3 centerPos)
    {
        GameObject ringRoot = new GameObject("EmpShockwaveRing");
        ringRoot.transform.position = centerPos + Vector3.up * 0.15f;

        Material empMat = IsometricBoardBuilder.GetOrCreateMaterial(
            "EmpPulseWave",
            new Color(0.22f, 0.98f, 0.78f),
            true,
            0.85f);

        // Build an 8-segment octagonal glowing energy ring
        const int segs = 8;
        Transform[] bars = new Transform[segs];
        for (int i = 0; i < segs; i++)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = $"WaveSeg_{i}";
            Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(ringRoot.transform, false);
            bar.GetComponent<Renderer>().sharedMaterial = empMat;
            bars[i] = bar.transform;
        }

        float duration = 0.42f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float radius = Mathf.Lerp(0.4f, 6.8f, Mathf.SmoothStep(0f, 1f, t));
            float thickness = Mathf.Lerp(0.22f, 0.03f, t);
            float segLen = radius * 0.85f;

            for (int i = 0; i < segs; i++)
            {
                float angleDeg = i * (360f / segs);
                float angleRad = angleDeg * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));
                bars[i].localPosition = dir * radius;
                bars[i].localRotation = Quaternion.Euler(0f, -angleDeg, 0f);
                bars[i].localScale = new Vector3(thickness, thickness * 0.7f, segLen);
            }

            yield return null;
        }

        Destroy(ringRoot);
    }

    public Tile GetTile(Vector2Int position)
    {
        if (tileGrid.Count == 0) RegisterAllTiles();
        tileGrid.TryGetValue(position, out Tile tile);
        return tile;
    }

    public StartTile GetStartTile()
    {
        if (startTile == null) RegisterAllTiles();
        return startTile;
    }

    public EndTile GetEndTile()
    {
        if (endTile == null) RegisterAllTiles();
        return endTile;
    }

    public Vector3 GridToWorld(Vector2Int gridPos, float yOffset = 0f)
    {
        Tile t = GetTile(gridPos);
        if (t != null)
            return t.transform.position + Vector3.up * yOffset;

        Vector3 origin = startTile != null
            ? startTile.transform.position - new Vector3(startTile.gridPosition.x * cellSpacing, 0f, startTile.gridPosition.y * cellSpacing)
            : Vector3.zero;
        return origin + new Vector3(gridPos.x * cellSpacing, yOffset, gridPos.y * cellSpacing);
    }
}
