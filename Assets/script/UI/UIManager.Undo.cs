using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIManager
{
    [Serializable] private class CircuitNodeState
    {
        public BlockType type;
        public int slot;
        public Vector2 position;
        public int next;
    }
    [Serializable] private class CircuitState
    {
        public List<CircuitNodeState> nodes = new List<CircuitNodeState>();
        public Vector2 startPosition, endPosition;
        public int startNext;
        public List<bool> paletteVisible = new List<bool>();
    }
    private readonly List<string> undoHistory = new List<string>();
    private Button undoButton;

    private void LateUpdate()
    {
        if (undoButton != null) undoButton.interactable = IsEditingAllowed && undoHistory.Count > 0;
    }

    private int NodeId(NodeCardUI node)
    {
        if (node == null) return -1;
        if (node == endNodeCard) return -2;
        return graphNodes.IndexOf(node);
    }

    private string CaptureCircuit()
    {
        var state = new CircuitState();
        state.startPosition = startNodeCard.rectTransform.anchoredPosition;
        state.endPosition = endNodeCard.rectTransform.anchoredPosition;
        state.startNext = NodeId(startNodeCard.connectedNext);
        foreach (var node in graphNodes)
            state.nodes.Add(new CircuitNodeState { type = node.blockType, slot = node.paletteSlotIndex,
                position = node.rectTransform.anchoredPosition, next = NodeId(node.connectedNext) });
        foreach (var card in paletteCards) state.paletteVisible.Add(card.gameObject.activeSelf);
        return JsonUtility.ToJson(state);
    }

    // Capture before an edit, including before a card starts moving.
    public void RecordCircuitEdit()
    {
        if (!Application.isPlaying || !IsEditingAllowed || nodesLayer == null || startNodeCard == null || endNodeCard == null) return;
        string snapshot = CaptureCircuit();
        if (undoHistory.Count > 0 && undoHistory[undoHistory.Count - 1] == snapshot) return;
        if (undoHistory.Count == 64) undoHistory.RemoveAt(0);
        undoHistory.Add(snapshot);
    }

    public void UndoLastEdit()
    {
        if (!IsEditingAllowed || nodesLayer == null || startNodeCard == null || endNodeCard == null) return;
        string current = CaptureCircuit();
        // A click without a movement, or an unchanged wire, is not an edit.
        while (undoHistory.Count > 0 && undoHistory[undoHistory.Count - 1] == current)
            undoHistory.RemoveAt(undoHistory.Count - 1);
        if (undoHistory.Count == 0) return;
        var state = JsonUtility.FromJson<CircuitState>(undoHistory[undoHistory.Count - 1]);
        undoHistory.RemoveAt(undoHistory.Count - 1);
        RestoreCircuit(state);
        SetStatusBanner("EDIT UNDONE — COMMAND BLOCKS AND WIRES RESTORED");
    }

    private void RestoreCircuit(CircuitState state)
    {
        ClearFailureFeedback();
        selectedClickPort = null;
        activeDragPort = null;
        wireConnector?.ClearPreviewWire();
        foreach (var node in graphNodes)
        {
            node.gameObject.SetActive(false);
            Destroy(node.gameObject);
        }
        graphNodes.Clear();
        startNodeCard.connectedNext = null;
        endNodeCard.connectedPrev = null;
        startNodeCard.rectTransform.anchoredPosition = state.startPosition;
        endNodeCard.rectTransform.anchoredPosition = state.endPosition;
        foreach (var saved in state.nodes)
        {
            var node = CreateNodeCardObject(saved.type, nodesLayer, false, false);
            node.paletteSlotIndex = saved.slot;
            node.rectTransform.anchoredPosition = saved.position;
            graphNodes.Add(node);
        }
        for (int i = 0; i < paletteCards.Count; i++)
            paletteCards[i].gameObject.SetActive(i < state.paletteVisible.Count && state.paletteVisible[i]);
        ReflowPaletteCards();
        RestoreUndoWire(startNodeCard, state.startNext);
        for (int i = 0; i < state.nodes.Count; i++) RestoreUndoWire(graphNodes[i], state.nodes[i].next);
        commands.Clear();
        RefreshWires();
    }

    private void RestoreUndoWire(NodeCardUI from, int next)
    {
        NodeCardUI to = next == -2 ? endNodeCard : next >= 0 && next < graphNodes.Count ? graphNodes[next] : null;
        from.connectedNext = to;
        if (to != null) to.connectedPrev = from;
    }
}
