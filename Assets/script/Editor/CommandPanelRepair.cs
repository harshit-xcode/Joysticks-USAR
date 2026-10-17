#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Repair the existing custom scene; preserve its level objects and command logic.
[InitializeOnLoad]
public static class CommandPanelRepair
{
    const string PendingKey = "Joysticks.CommandPanelRepair.fixed-endpoints.v2";

    static CommandPanelRepair()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.delayCall += TryAutomaticRepair;
    }

    static void OnPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += TryAutomaticRepair;
    }

    static void TryAutomaticRepair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(PendingKey, false)) return;
        var manager = Object.FindFirstObjectByType<UIManager>();
        if (manager == null) return;
        if (Repair(manager)) SessionState.SetBool(PendingKey, true);
    }

    [MenuItem("Tools/Joysticks/Repair Command Panel")]
    public static void RepairFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before repairing the command panel.");
            return;
        }
        var manager = Object.FindFirstObjectByType<UIManager>();
        if (manager == null) { Debug.LogWarning("Open your custom level scene containing UIManager first."); return; }
        Repair(manager);
    }

    static bool Repair(UIManager manager)
    {
        var scene = manager.gameObject.scene;
        var blocks = Object.FindObjectsByType<BlockDragDrop>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(b => b.gameObject.scene == scene).OrderBy(b => b.blockType).ToArray();
        if (blocks.Length == 0) return false;
        var canvas = blocks[0].GetComponentInParent<Canvas>(true);
        if (canvas == null) { Debug.LogWarning("Command buttons must be under a Canvas."); return false; }
        var palette = blocks[0].transform.parent as RectTransform;
        var slots = manager.sequenceSlots.Where(s => s != null).GroupBy(s => s.gameObject).Select(g => g.First()).ToArray();
        if (palette == null || slots.Length == 0) return false;
        var sequence = slots[0].transform.parent as RectTransform;
        if (sequence == null || sequence == palette) return false;
        // Only repair the known structure: separate palette/sequence siblings under a Canvas.
        if (palette.parent != canvas.transform || sequence.parent != canvas.transform)
        { Debug.LogWarning("Command layout differs from the expected Canvas/PalettePanel/SequencePanel structure."); return false; }

        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Repair command palette and program slots");
        Undo.RecordObject(canvas, "Show UI Canvas");
        canvas.enabled = true;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Undo.RecordObject(canvas.transform, "Reset Canvas scale");
        canvas.transform.localScale = Vector3.one;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        Undo.RecordObject(scaler, "Scale command interface");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1440, 900);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;

        Panel(palette, new Vector2(0, 0), new Vector2(.24f, 1));
        Panel(sequence, new Vector2(.76f, 0), new Vector2(1, 1));
        Heading(palette, "Commands heading", "COMMANDS");
        Heading(sequence, "Program heading", "YOUR PROGRAM");

        for (int i = 0; i < blocks.Length; i++)
        {
            var block = blocks[i];
            if (block.transform.parent != palette) continue;
            Row(block.transform as RectTransform, -65 - i * 68, 52);
            var image = block.GetComponent<Image>();
            if (image != null)
            {
                Undo.RecordObject(image, "Show command button");
                image.enabled = true;
                image.raycastTarget = true;
                image.color = block.blockType == BlockType.Lava ? new Color(.8f, .3f, .1f) : new Color(.15f, .4f, .7f);
            }
            Label(block.transform, "Command label", CommandLabels.For(block.blockType), Color.white);
        }
        for (int i = 0; i < slots.Length; i++)
        {
            Row(slots[i].transform as RectTransform, -65 - i * 64, 50);
            var image = slots[i].GetComponent<Image>();
            if (image != null)
            {
                Undo.RecordObject(image, "Show program slot");
                image.enabled = true;
                image.raycastTarget = true;
                image.color = new Color(.8f, .85f, .9f);
            }
            var label = Label(slots[i].transform, "Slot label", "Empty", Color.black);
            foreach (var slot in slots[i].GetComponents<BlockSlot>())
            {
                Undo.RecordObject(slot, "Connect slot visuals");
                slot.slotBackground = image;
                slot.slotLabel = label;
            }
        }
        foreach (var button in sequence.GetComponentsInChildren<RunButton>(true))
        {
            Row(button.transform as RectTransform, -85 - slots.Length * 64, 52);
            Label(button.transform, "Run label", "RUN", Color.black);
        }
        // The final frame reserves independent START/STOP rows outside the editable list.
        foreach (var rect in sequence.GetComponentsInChildren<RectTransform>(true))
            Undo.RecordObject(rect, "Position fixed program frame");
        foreach (var block in blocks) Undo.RecordObject(block.gameObject, "Hide automatic boundary commands");
        var oldStart = sequence.Find("Fixed Start");
        var oldStop = sequence.Find("Fixed Stop");
        manager.ConfigureFixedProgramFrame();
        if (oldStart == null && sequence.Find("Fixed Start") != null)
            Undo.RegisterCreatedObjectUndo(sequence.Find("Fixed Start").gameObject, "Create permanent START");
        if (oldStop == null && sequence.Find("Fixed Stop") != null)
            Undo.RegisterCreatedObjectUndo(sequence.Find("Fixed Stop").gameObject, "Create permanent STOP");
        Canvas.ForceUpdateCanvases();
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Command panel repaired. Commands are on the left; program slots and RUN are on the right. Save the scene, then press Play.");
        return true;
    }

    static void Panel(RectTransform rect, Vector2 min, Vector2 max)
    {
        Undo.RecordObject(rect, "Position side panel");
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        if (!rect.gameObject.activeSelf) { Undo.RecordObject(rect.gameObject, "Enable panel"); rect.gameObject.SetActive(true); }
        var image = rect.GetComponent<Image>();
        if (image != null)
        {
            Undo.RecordObject(image, "Style side panel");
            image.color = new Color(.07f, .1f, .16f, .96f);
            image.raycastTarget = false;
        }
    }

    static void Row(RectTransform rect, float y, float height)
    {
        if (rect == null) return;
        Undo.RecordObject(rect, "Position UI row");
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(-28, height);
        rect.localScale = Vector3.one;
    }

    static void Heading(RectTransform parent, string name, string text)
    {
        var existing = parent.Find(name);
        var label = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
        if (label == null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(obj, "Create panel heading");
            obj.transform.SetParent(parent, false);
            label = obj.GetComponent<TextMeshProUGUI>();
        }
        Row(label.rectTransform, -15, 35);
        Undo.RecordObject(label, "Name panel heading");
        label.text = text; label.fontSize = 24; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, Color color)
    {
        var label = parent.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(obj, "Create command label");
            obj.transform.SetParent(parent, false);
            label = obj.GetComponent<TextMeshProUGUI>();
        }
        Undo.RecordObject(label, "Set command label");
        Undo.RecordObject(label.rectTransform, "Fit command label");
        label.text = text; label.fontSize = 22; label.color = color;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        return label;
    }
}
#endif
