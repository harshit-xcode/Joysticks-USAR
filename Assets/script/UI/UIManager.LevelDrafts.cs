using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class UIManager
{
    public const string CircuitDraftKeyPrefix = "Joysticks.CircuitDraft.Level.";
    [Serializable] private class LevelCircuitDraft
    {
        public int version = 1;
        public string title;
        public string palette;
        public CircuitState circuit;
    }
    private int displayedDraftLevel = -1;
    private string displayedDraftTitle;
    private readonly Dictionary<int, List<string>> levelUndoHistories = new Dictionary<int, List<string>>();

    private string PaletteSignature() => string.Join(",", paletteCards.Select(card => ((int)card.blockType) + ":" + card.paletteSlotIndex));

    public void SaveDisplayedLevelDraft()
    {
        if (!Application.isPlaying || displayedDraftLevel < 0 || nodesLayer == null || startNodeCard == null || endNodeCard == null) return;
        if (graphNodes.Any(node => node == null) || paletteCards.Any(card => card == null)) return;
        var draft = new LevelCircuitDraft { title = displayedDraftTitle, palette = PaletteSignature(),
            circuit = JsonUtility.FromJson<CircuitState>(CaptureCircuit()) };
        PlayerPrefs.SetString(CircuitDraftKeyPrefix + (displayedDraftLevel + 1), JsonUtility.ToJson(draft));
        PlayerPrefs.Save();
        levelUndoHistories[displayedDraftLevel] = new List<string>(undoHistory);
    }

    private void RestoreLevelDraft(LevelDefinition level, int index)
    {
        displayedDraftLevel = index;
        displayedDraftTitle = level != null ? level.title : "";
        if (!Application.isPlaying) return;
        string saved = PlayerPrefs.GetString(CircuitDraftKeyPrefix + (index + 1), "");
        if (string.IsNullOrEmpty(saved)) return;
        LevelCircuitDraft draft;
        try { draft = JsonUtility.FromJson<LevelCircuitDraft>(saved); }
        catch (ArgumentException) { return; }
        // Ignore drafts whose level/palette changed rather than loading obsolete command stock.
        if (draft == null || draft.version != 1 || draft.title != displayedDraftTitle || draft.palette != PaletteSignature()
            || !IsValidCircuitDraft(draft.circuit)) return;
        RestoreCircuit(draft.circuit);
        if (levelUndoHistories.TryGetValue(index, out var history)) undoHistory.AddRange(history);
        SetStatusBanner("CIRCUIT RESTORED — YOUR SAVED BLOCKS AND WIRES ARE READY");
    }

    private bool IsValidCircuitDraft(CircuitState state)
    {
        if (state == null || state.nodes == null || state.paletteVisible == null || state.nodes.Count > 64
            || state.paletteVisible.Count != paletteCards.Count || !ValidDraftPosition(state.startPosition) || !ValidDraftPosition(state.endPosition)) return false;
        int count = state.nodes.Count;
        if (state.startNext < -2 || state.startNext >= count) return false;
        var usedSlots = new HashSet<int>();
        var incoming = new HashSet<int>();
        if (state.startNext >= 0) incoming.Add(state.startNext);
        for (int i = 0; i < count; i++)
        {
            var node = state.nodes[i];
            if (node == null || !ProgramSequence.IsEditableCommand(node.type) || !ValidDraftPosition(node.position)
                || node.slot < -1 || node.slot >= paletteCards.Count || node.next < -2 || node.next >= count || node.next == i) return false;
            if (node.slot >= 0 && (!usedSlots.Add(node.slot) || paletteCards[node.slot].blockType != node.type)) return false;
            if (node.next >= 0 && !incoming.Add(node.next)) return false;
            var visited = new HashSet<int>();
            int link = i;
            while (link >= 0)
            {
                if (link >= count || !visited.Add(link) || state.nodes[link] == null) return false;
                link = state.nodes[link].next;
            }
        }
        for (int slot = 0; slot < paletteCards.Count; slot++)
            if (state.paletteVisible[slot] == usedSlots.Contains(slot)) return false;
        return true;
    }

    private static bool ValidDraftPosition(Vector2 value) => !float.IsNaN(value.x) && !float.IsNaN(value.y)
        && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);

    private void OnApplicationPause(bool paused) { if (paused) SaveDisplayedLevelDraft(); }
    private void OnApplicationQuit() { SaveDisplayedLevelDraft(); }
}
