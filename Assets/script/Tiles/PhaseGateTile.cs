// PhaseGateTile.cs
// Dynamic Laser Phase Barrier tile that toggles between CLOSED (acts as an Obstacle wall)
// and OPEN (walkable Free tile) whenever the robot executes a [PULSE] node!

using System.Collections.Generic;
using UnityEngine;

public class PhaseGateTile : Tile
{
    [Header("Phase Gate Settings")]
    public bool startsClosed = true;
    public Transform laserBeamsRoot;
    public Transform leftOrbTransform;
    public Transform rightOrbTransform;
    public List<Renderer> beaconRenderers = new List<Renderer>();

    private Material closedBeaconMat;
    private Material openBeaconMat;
    private float currentBeamScale = 1f;

    private void Awake()
    {
        tileType = startsClosed ? TileType.PhaseGateClosed : TileType.PhaseGateOpen;
        currentBeamScale = startsClosed ? 1f : 0f;
    }

    public bool IsClosedRightNow()
    {
        bool flipped = TileManager.Instance != null && TileManager.Instance.IsGateFlipped;
        return startsClosed ? !flipped : flipped;
    }

    public override bool CanPlayerEnter(PlayerController player)
    {
        // When CLOSED, this gate acts as a solid Obstacle wall (clamping Walk, Jump, Left, Right)!
        // When OPEN, the player can freely walk or jump onto/over it!
        return !IsClosedRightNow();
    }

    public override void OnPlayerEnter(PlayerController player)
    {
        if (player == null) return;

        if (IsClosedRightNow())
        {
            Debug.Log($"PhaseGateTile at {gridPosition} slammed CLOSED on the player — respawning!");
            AudioManager.Instance?.PlayLavaFail();
            PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
            if (respawn != null)
                respawn.Respawn();
            else
                player.PlaceOnStartTile();
        }
        else
        {
            Debug.Log($"Player passed through OPEN PhaseGateTile at {gridPosition}.");
        }
    }

    public void SyncVisualImmediate()
    {
        bool closed = IsClosedRightNow();
        currentBeamScale = closed ? 1f : 0f;
        if (laserBeamsRoot != null)
        {
            laserBeamsRoot.localScale = new Vector3(currentBeamScale, 1f, 1f);
            laserBeamsRoot.gameObject.SetActive(closed);
        }
        UpdateBeacons(closed);
    }

    private void Update()
    {
        bool closed = IsClosedRightNow();

        // Rotate twin pylon emitter cores continuously
        float spin = (closed ? 180f : 80f) * Time.deltaTime;
        if (leftOrbTransform != null)
            leftOrbTransform.Rotate(spin, spin * 0.7f, 0f, Space.Self);
        if (rightOrbTransform != null)
            rightOrbTransform.Rotate(-spin, spin * 0.7f, 0f, Space.Self);

        // Animate horizontal laser barrier beams retracting / igniting + subtle energy hum
        if (laserBeamsRoot != null)
        {
            float targetScale = closed ? (0.98f + Mathf.Sin(Time.time * 14f) * 0.03f) : 0f;
            currentBeamScale = Mathf.Lerp(currentBeamScale, targetScale, Time.deltaTime * 16f);
            bool visible = currentBeamScale > 0.03f;
            if (laserBeamsRoot.gameObject.activeSelf != visible)
                laserBeamsRoot.gameObject.SetActive(visible);
            if (visible)
                laserBeamsRoot.localScale = new Vector3(currentBeamScale, 1f + Mathf.Sin(Time.time * 18f) * 0.08f, 1f);
        }

        UpdateBeacons(closed);
    }

    private void UpdateBeacons(bool closed)
    {
        if (beaconRenderers == null || beaconRenderers.Count == 0) return;

        if (closedBeaconMat == null)
            closedBeaconMat = IsometricBoardBuilder.GetOrCreateMaterial("GateClosedBeacon", new Color(1.00f, 0.22f, 0.32f), true, 0.75f);
        if (openBeaconMat == null)
            openBeaconMat = IsometricBoardBuilder.GetOrCreateMaterial("GateOpenBeacon", new Color(0.18f, 0.96f, 0.62f), true, 0.75f);

        Material targetMat = closed ? closedBeaconMat : openBeaconMat;
        for (int i = 0; i < beaconRenderers.Count; i++)
        {
            var r = beaconRenderers[i];
            if (r != null && r.sharedMaterial != targetMat)
                r.sharedMaterial = targetMat;
        }
    }
}
