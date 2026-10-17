// PhaseSpikeTile.cs
// Dynamic hydraulic plasma-spike tile that alternates between EXTENDED (lethal) and RETRACTED (safe) on every beat.
// Features continuous 3D turbine rotation, smooth hydraulic piston travel, and glowing status rims.

using System.Collections.Generic;
using UnityEngine;

public class PhaseSpikeTile : Tile
{
    [Header("Phase Spike Settings")]
    public bool startsExtended = true;
    public Transform spikeRackTransform;
    public Transform rotatingCoreTransform;
    public List<Renderer> statusRimRenderers = new List<Renderer>();

    private Material armedRimMat;
    private Material safeRimMat;
    private float currentRackY = 0f;

    private void Awake()
    {
        tileType = startsExtended ? TileType.PhaseSpikeUp : TileType.PhaseSpikeDown;
        currentRackY = startsExtended ? 0f : -0.34f;
    }

    public bool IsLethalOnCurrentBeat()
    {
        int beat = TileManager.Instance != null ? TileManager.Instance.CurrentBeat : 0;
        bool evenBeat = (beat % 2 == 0);
        return startsExtended ? evenBeat : !evenBeat;
    }

    public override bool CanPlayerEnter(PlayerController player)
    {
        return !hasObstacle;
    }

    public override void OnPlayerEnter(PlayerController player)
    {
        if (player == null) return;

        if (IsLethalOnCurrentBeat())
        {
            Debug.Log($"PhaseSpikeTile at {gridPosition} is EXTENDED on beat {(TileManager.Instance != null ? TileManager.Instance.CurrentBeat : 0)} — player impaled!");
            TriggerRespawn(player);
        }
        else
        {
            Debug.Log($"PhaseSpikeTile at {gridPosition} is RETRACTED (safe) on beat {(TileManager.Instance != null ? TileManager.Instance.CurrentBeat : 0)}.");
        }
    }

    public void TriggerRespawn(PlayerController player)
    {
        if (player == null || player.currentState == PlayerState.Dead) return;
        AudioManager.Instance?.PlayLavaFail();
        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null)
            respawn.Respawn();
        else
            player.PlaceOnStartTile();
    }

    public void SyncVisualImmediate()
    {
        bool extended = IsLethalOnCurrentBeat();
        currentRackY = extended ? 0f : -0.34f;
        if (spikeRackTransform != null)
        {
            Vector3 lp = spikeRackTransform.localPosition;
            lp.y = currentRackY;
            spikeRackTransform.localPosition = lp;
        }
        UpdateRimMaterial(extended);
    }

    private void Update()
    {
        bool extended = IsLethalOnCurrentBeat();

        // Continuous spinning energy core so the board looks alive even while planning!
        if (rotatingCoreTransform != null)
        {
            float spinSpeed = extended ? 160f : 75f;
            rotatingCoreTransform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        // Smooth hydraulic piston slide + subtle idle breathing when extended
        if (spikeRackTransform != null)
        {
            float bob = extended ? Mathf.Sin(Time.time * 6.5f) * 0.025f : 0f;
            float targetY = (extended ? 0.02f : -0.34f) + bob;
            currentRackY = Mathf.Lerp(currentRackY, targetY, Time.deltaTime * 14f);
            Vector3 lp = spikeRackTransform.localPosition;
            lp.y = currentRackY;
            spikeRackTransform.localPosition = lp;
        }

        UpdateRimMaterial(extended);
    }

    private void UpdateRimMaterial(bool extended)
    {
        if (statusRimRenderers == null || statusRimRenderers.Count == 0) return;

        if (armedRimMat == null)
            armedRimMat = IsometricBoardBuilder.GetOrCreateMaterial("PhaseSpikeArmedRim", new Color(1.00f, 0.16f, 0.36f), true, 0.7f);
        if (safeRimMat == null)
            safeRimMat = IsometricBoardBuilder.GetOrCreateMaterial("PhaseSpikeSafeRim", new Color(0.00f, 0.90f, 1.00f), true, 0.7f);

        Material targetMat = extended ? armedRimMat : safeRimMat;
        for (int i = 0; i < statusRimRenderers.Count; i++)
        {
            var r = statusRimRenderers[i];
            if (r != null && r.sharedMaterial != targetMat)
                r.sharedMaterial = targetMat;
        }
    }
}
