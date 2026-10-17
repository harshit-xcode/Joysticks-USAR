// UILineConnector.cs
// Custom UI Graphic that renders smooth S-curve Bezier circuit wires and live drag-wire previews
// inside the LOOP CIRCUIT BOARD panel.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct WireSegment
{
    public Vector2 start;
    public Vector2 end;
    public Color color;
    public float thickness;
    public bool isHighlighted;

    public WireSegment(Vector2 start, Vector2 end, Color color, float thickness = 4.6f, bool isHighlighted = false)
    {
        this.start = start;
        this.end = end;
        this.color = color;
        this.thickness = thickness;
        this.isHighlighted = isHighlighted;
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public class UILineConnector : MaskableGraphic
{
    private readonly List<WireSegment> segments = new List<WireSegment>();
    private bool hasPreviewWire = false;
    private WireSegment previewSegment;

    public void SetWires(IEnumerable<WireSegment> newSegments)
    {
        segments.Clear();
        if (newSegments != null)
            segments.AddRange(newSegments);
        SetVerticesDirty();
    }

    public void SetPreviewWire(Vector2 start, Vector2 end, Color color)
    {
        hasPreviewWire = true;
        previewSegment = new WireSegment(start, end, color, 5.0f, true);
        SetVerticesDirty();
    }

    public void ClearPreviewWire()
    {
        if (!hasPreviewWire) return;
        hasPreviewWire = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        for (int i = 0; i < segments.Count; i++)
        {
            DrawBezierWire(vh, segments[i]);
        }

        if (hasPreviewWire)
        {
            DrawBezierWire(vh, previewSegment);
        }
    }

    private void DrawBezierWire(VertexHelper vh, WireSegment seg)
    {
        float dist = Vector2.Distance(seg.start, seg.end);
        if (dist < 1f) return;

        float tangentOffset = Mathf.Clamp(Mathf.Abs(seg.end.x - seg.start.x) * 0.48f, 38f, 160f);
        Vector2 c0 = seg.start + new Vector2(tangentOffset, 0f);
        Vector2 c1 = seg.end - new Vector2(tangentOffset, 0f);

        const int steps = 18;
        Color shadowCol = seg.isHighlighted
            ? new Color(0.45f, 1.0f, 0.78f, 0.50f)
            : new Color(0.02f, 0.04f, 0.08f, 0.90f);

        Color mainCol = seg.isHighlighted
            ? new Color(0.45f, 1.0f, 0.78f, 1.0f)
            : seg.color;

        float outerHalf = (seg.thickness + 3.6f) * 0.5f;
        float innerHalf = seg.thickness * 0.5f;

        Vector2 prev = seg.start;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 curr = EvaluateCubicBezier(seg.start, c0, c1, seg.end, t);
            Vector2 diff = curr - prev;
            float len = diff.magnitude;
            if (len > 0.01f)
            {
                Vector2 normal = new Vector2(-diff.y / len, diff.x / len);
                AddQuad(vh, prev, curr, normal, outerHalf, shadowCol);
                AddQuad(vh, prev, curr, normal, innerHalf, mainCol);
            }
            prev = curr;
        }
    }

    private static Vector2 EvaluateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
    }

    private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 normal, float halfWidth, Color col)
    {
        int idx = vh.currentVertCount;

        UIVertex v0 = UIVertex.simpleVert;
        v0.position = a - normal * halfWidth;
        v0.color = col;

        UIVertex v1 = UIVertex.simpleVert;
        v1.position = a + normal * halfWidth;
        v1.color = col;

        UIVertex v2 = UIVertex.simpleVert;
        v2.position = b + normal * halfWidth;
        v2.color = col;

        UIVertex v3 = UIVertex.simpleVert;
        v3.position = b - normal * halfWidth;
        v3.color = col;

        vh.AddVert(v0);
        vh.AddVert(v1);
        vh.AddVert(v2);
        vh.AddVert(v3);

        vh.AddTriangle(idx + 0, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx + 0);
    }
}
