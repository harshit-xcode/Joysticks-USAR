using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    [Header("References")]
    public CodeExecutor codeExecutor;
    public PlayerController player;
    [Tooltip("Optional. Existing scenes infer this from the old sequence slots.")]
    public RectTransform programPanel;

    [Header("Legacy scene references (used only to locate and hide the old slots)")]
    public List<BlockSlot> sequenceSlots = new List<BlockSlot>();

    [Header("Block Prefabs")]
    public StartBlock startBlockPrefab;
    public WalkBlock walkBlockPrefab;
    [InspectorName("Jump Block Prefab")] public LavaBlock lavaBlockPrefab;
    public StopBlock stopBlockPrefab;

    readonly ProgramCommandList commands = new ProgramCommandList();
    readonly List<GameObject> renderedRows = new List<GameObject>();
    readonly List<CodeBlock> spawnedCommands = new List<CodeBlock>();
    ScrollRect programScroll;
    RectTransform content;
    public int CommandCount => commands.Count;
    bool EditingAllowed => codeExecutor == null || !codeExecutor.IsRunning();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() { ConfigureFixedProgramFrame(); }
    void OnDestroy() { if (Instance == this) Instance = null; }

    // Keep this entry point compatible with the editor layout repair menu.
    public void ConfigureFixedProgramFrame()
    {
        if (!Application.isPlaying) return;
        if (programPanel == null)
            foreach (var slot in sequenceSlots)
                if (slot != null) { programPanel = slot.transform.parent as RectTransform; break; }
        if (programPanel == null)
        {
            Debug.LogError("UIManager: Assign the YOUR PROGRAM panel in the Inspector.", this);
            return;
        }

        foreach (var slot in sequenceSlots)
            if (slot != null) slot.gameObject.SetActive(false);
        HideLegacyBoundary("Fixed Start");
        HideLegacyBoundary("Fixed Stop");

        if (programScroll == null) CreateScrollArea();
        foreach (var run in programPanel.GetComponentsInChildren<RunButton>(true))
        {
            var rect = run.transform as RectTransform;
            if (rect == null) continue;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 20);
            rect.sizeDelta = new Vector2(-20, 50);
            rect.localScale = Vector3.one;
        }
        ConfigureResetButton();
        var canvas = programPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            int index = 0;
            foreach (var block in canvas.GetComponentsInChildren<BlockDragDrop>(true))
            {
                bool editable = !ProgramSequence.IsBoundary(block.blockType);
                block.gameObject.SetActive(editable);
                if (editable)
                {
                    FixedProgramFrame.PositionRow(block.transform as RectTransform, -65 - index++ * 68);
                    var label = block.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null) label.text = CommandLabels.For(block.blockType);
                    var oldLabel = block.GetComponentInChildren<Text>(true);
                    if (oldLabel != null) oldLabel.text = CommandLabels.For(block.blockType);
                }
            }
        }
        RenderProgram(false);
    }

    void ConfigureResetButton()
    {
        var existing = programPanel.Find("Reset Button");
        GameObject obj;
        if (existing == null)
        {
            obj = new GameObject("Reset Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(programPanel, false);
            CreateLabel(obj.transform, "RESET");
        }
        else obj = existing.gameObject;
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.anchoredPosition = new Vector2(0, 20);
        rect.sizeDelta = new Vector2(-20, 50);
        rect.localScale = Vector3.one;
        var image = obj.GetComponent<Image>();
        image.color = new Color(.25f, .35f, .5f);
        var button = obj.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveListener(ResetProgram);
        button.onClick.AddListener(ResetProgram);
    }

    // Reset is available during execution and after victory; the editable program stays intact.
    public void ResetProgram()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.ResetForRetry();
        else
        {
            if (codeExecutor != null) codeExecutor.StopExecution();
            var robot = player != null ? player : codeExecutor != null ? codeExecutor.player : null;
            if (robot != null)
            {
                robot.GetComponent<PlayerRespawn>()?.CancelPendingRespawn();
                robot.PlaceOnStartTile();
            }
        }
        foreach (var old in spawnedCommands) if (old != null) Destroy(old.gameObject);
        spawnedCommands.Clear();
    }

    void HideLegacyBoundary(string name)
    {
        var old = programPanel.Find(name);
        if (old != null) old.gameObject.SetActive(false);
    }

    void CreateScrollArea()
    {
        var root = new GameObject("Growing Program", typeof(RectTransform), typeof(ScrollRect));
        root.transform.SetParent(programPanel, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12, 90);
        rect.offsetMax = new Vector2(-12, -60);
        programScroll = root.GetComponent<ScrollRect>();
        programScroll.horizontal = false;
        programScroll.vertical = true;
        programScroll.movementType = ScrollRect.MovementType.Clamped;
        programScroll.scrollSensitivity = 35;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D), typeof(ProgramDropTarget));
        viewportObject.transform.SetParent(root.transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        Stretch(viewport);
        viewportObject.GetComponent<Image>().color = new Color(0, 0, 0, .08f);
        viewportObject.GetComponent<ProgramDropTarget>().owner = this;
        programScroll.viewport = viewport;

        var contentObject = new GameObject("Commands", typeof(RectTransform));
        contentObject.transform.SetParent(viewport, false);
        content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1);
        content.anchoredPosition = Vector2.zero;
        programScroll.content = content;
    }

    public bool TryAddBlock(BlockType type)
    {
        if (!EditingAllowed || ProgramSequence.IsBoundary(type)) return false;
        if (content == null) ConfigureFixedProgramFrame();
        if (content == null || !commands.Append(type)) return false;
        RenderProgram(true);
        return true;
    }

    public void RemoveCommandAt(int index)
    {
        if (!EditingAllowed || !commands.RemoveAt(index)) return;
        RenderProgram(false);
    }

    // Kept for LevelManager.RestartLevel; there are no empty slots to refill.
    public void ClearAllSlots()
    {
        commands.Clear();
        RenderProgram(false);
    }

    void RenderProgram(bool scrollToBottom)
    {
        if (content == null) return;
        float previousScroll = programScroll.verticalNormalizedPosition;
        foreach (var row in renderedRows)
            if (row != null) { row.SetActive(false); Destroy(row); }
        renderedRows.Clear();
        AddRow("START (fixed)", 0, new Color(.12f, .55f, .3f), -1);
        for (int i = 0; i < commands.Count; i++)
        {
            var kind = commands.Commands[i];
            AddRow((i + 1) + ". " + CommandLabels.For(kind), i + 1,
                kind == BlockType.Lava ? new Color(.8f, .3f, .1f) : new Color(.15f, .4f, .7f), i);
        }
        AddRow("STOP (at goal)", commands.Count + 1, new Color(.7f, .2f, .2f), -1);
        content.sizeDelta = new Vector2(0, (commands.Count + 2) * 62 + 4);
        Canvas.ForceUpdateCanvases();
        programScroll.StopMovement();
        programScroll.verticalNormalizedPosition = scrollToBottom ? 0 : previousScroll;
    }

    void AddRow(string caption, int position, Color color, int index)
    {
        var row = new GameObject(caption, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        row.transform.SetParent(content, false);
        renderedRows.Add(row);
        var rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -position * 62);
        rect.sizeDelta = new Vector2(-8, 50);
        row.GetComponent<Image>().color = color;
        var text = CreateLabel(row.transform, caption);
        text.rectTransform.offsetMin = new Vector2(10, 0);
        text.rectTransform.offsetMax = new Vector2(index >= 0 ? -50 : -10, 0);
        if (index < 0) return;

        var actions = row.AddComponent<ProgramCommandRow>();
        actions.owner = this;
        actions.commandIndex = index;
        var remove = new GameObject("Remove command", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        remove.transform.SetParent(row.transform, false);
        var removeRect = remove.GetComponent<RectTransform>();
        removeRect.anchorMin = new Vector2(1, 0);
        removeRect.anchorMax = Vector2.one;
        removeRect.pivot = new Vector2(1, .5f);
        removeRect.anchoredPosition = new Vector2(-5, 0);
        removeRect.sizeDelta = new Vector2(36, -10);
        remove.GetComponent<Image>().color = new Color(.1f, .15f, .2f, .7f);
        remove.GetComponent<Button>().onClick.AddListener(() => RemoveCommandAt(index));
        CreateLabel(remove.transform, "x");
    }

    static TextMeshProUGUI CreateLabel(Transform parent, string caption)
    {
        var obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.text = caption;
        text.fontSize = 21;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        Stretch(text.rectTransform);
        return text;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public void CollectBlocksAndRun()
    {
        if (codeExecutor == null || codeExecutor.IsRunning()) return;
        if (commands.Count == 0) { Debug.LogWarning("Click Walk or Jump to add commands.", this); return; }
        var ordered = commands.BuildSequence();
        foreach (var type in ordered)
            if (GetPrefab(type) == null)
            { Debug.LogError("Assign the " + CommandLabels.For(type) + " block prefab in UIManager.", this); return; }

        // These hidden command instances belong to the UI; replace them between runs.
        foreach (var old in spawnedCommands) if (old != null) Destroy(old.gameObject);
        spawnedCommands.Clear();
        foreach (var type in ordered)
        {
            var block = Instantiate(GetPrefab(type), transform);
            block.gameObject.hideFlags = HideFlags.HideInHierarchy;
            spawnedCommands.Add(block);
        }
        codeExecutor.Run(new List<CodeBlock>(spawnedCommands));
    }

    CodeBlock GetPrefab(BlockType type)
    {
        switch (type)
        {
            case BlockType.Start: return startBlockPrefab;
            case BlockType.Walk: return walkBlockPrefab;
            case BlockType.Lava: return lavaBlockPrefab;
            case BlockType.Stop: return stopBlockPrefab;
            default: return null;
        }
    }
}
