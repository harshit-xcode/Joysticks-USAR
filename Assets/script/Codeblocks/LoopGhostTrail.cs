using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// One bounded world-space path per execution pass. No colliders or gameplay effects.
public sealed class LoopGhostTrail : MonoBehaviour
{
    const int MaxPasses = 12;
    const int MaxPoints = 256;
    readonly List<LineRenderer> trails = new List<LineRenderer>();
    readonly List<Vector3> points = new List<Vector3>();
    static readonly Color[] PassColors = {
        new Color(.22f, .88f, 1f), new Color(.78f, .53f, 1f),
        new Color(1f, .76f, .30f), new Color(.32f, 1f, .69f)
    };
    PlayerController player;
    LineRenderer activeTrail;
    Material trailMaterial;
    int currentPass;

    public int TrailCount => trails.Count;

    public void BeginRun(PlayerController robot)
    {
        ClearTrails();
        player = robot;
        BeginPass(1);
    }

    public void BeginPass(int pass)
    {
        FinishPass();
        if (player == null) return;
        currentPass = pass;
        if (trailMaterial == null)
        {
            var shader = Resources.Load<Shader>("LoopGhostTrail");
            if (shader == null) { Debug.LogWarning("Loop ghost trail shader is missing.", this); return; }
            trailMaterial = new Material(shader) { name = "Loop Ghost Trail Material" };
        }
        if (trails.Count == MaxPasses)
        {
            trails[0].gameObject.SetActive(false);
            Destroy(trails[0].gameObject);
            trails.RemoveAt(0);
        }
        var go = new GameObject("Ghost Trail - Pass " + pass);
        go.transform.SetParent(transform, false);
        activeTrail = go.AddComponent<LineRenderer>();
        activeTrail.useWorldSpace = true;
        activeTrail.sharedMaterial = trailMaterial;
        activeTrail.alignment = LineAlignment.View;
        activeTrail.numCapVertices = 3;
        activeTrail.numCornerVertices = 3;
        activeTrail.shadowCastingMode = ShadowCastingMode.Off;
        activeTrail.receiveShadows = false;
        activeTrail.positionCount = 0;
        activeTrail.startColor = activeTrail.endColor = PassColors[(pass - 1) % PassColors.Length];
        trails.Add(activeTrail);
        points.Clear();
        SamplePosition();
        UpdateFading();
    }

    void LateUpdate()
    {
        // Do not record a death/respawn teleport as a line across the board.
        if (activeTrail != null && player != null && player.currentState != PlayerState.Dead)
            SamplePosition();
    }

    void SamplePosition()
    {
        if (activeTrail == null || player == null) return;
        Vector3 position = player.transform.position + Vector3.up * (.04f + (currentPass % MaxPasses) * .004f);
        if (points.Count > 0 && (position - points[points.Count - 1]).sqrMagnitude < .0025f) return;
        if (points.Count < MaxPoints) points.Add(position);
        else points[points.Count - 1] = position;
        activeTrail.positionCount = points.Count;
        activeTrail.SetPosition(points.Count - 1, position);
    }

    public void FinishPass()
    {
        if (activeTrail == null) return;
        if (player != null && player.currentState != PlayerState.Dead) SamplePosition();
        if (points.Count < 2)
        {
            trails.Remove(activeTrail);
            activeTrail.gameObject.SetActive(false);
            Destroy(activeTrail.gameObject);
        }
        activeTrail = null;
        points.Clear();
        UpdateFading();
    }

    void UpdateFading()
    {
        for (int i = 0; i < trails.Count; i++)
        {
            int age = trails.Count - 1 - i;
            Color color = trails[i].startColor;
            color.a = Mathf.Max(.08f, .34f - age * .025f);
            trails[i].startColor = trails[i].endColor = color;
            // Older paths remain visible around the edges when passes overlap exactly.
            trails[i].widthMultiplier = .10f + age * .012f;
        }
    }

    public void ClearTrails()
    {
        activeTrail = null;
        points.Clear();
        foreach (var trail in trails)
            if (trail != null) { trail.gameObject.SetActive(false); Destroy(trail.gameObject); }
        trails.Clear();
        player = null;
    }

    void OnDisable() { ClearTrails(); }
    void OnDestroy() { if (trailMaterial != null) Destroy(trailMaterial); }
}
