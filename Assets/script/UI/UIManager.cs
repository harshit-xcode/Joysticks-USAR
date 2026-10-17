// UIManager.cs
// Drives the split-screen Isometric 3D + Node-Graph interface:
// - Top half: 3D Isometric scene viewport with "DEPLOYMENT FACTORY - TEST X" header & level controls
// - Center divider: Play / Stop / Reset / Auto-Wire controls
// - Bottom-left: "NODE MODIFIERS" draggable node palette
// - Bottom-right: "MACHINE LEARNING LAB" interactive node-graph workspace with START, END, and glowing wires

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Core References")]
    public CodeExecutor codeExecutor;
    public PlayerController player;
    public RectTransform programPanel;

    [Header("Legacy Scene References (Preserved for Compatibility)")]
    public List<BlockSlot> sequenceSlots = new List<BlockSlot>();

    [Header("Block Prefabs (Optional — UniversalCodeBlock fallback used automatically)")]
    public StartBlock startBlockPrefab;
    public WalkBlock walkBlockPrefab;
    [InspectorName("Jump Block Prefab")] public LavaBlock lavaBlockPrefab;
    public StopBlock stopBlockPrefab;
    public LeftBlock leftBlockPrefab;
    public RightBlock rightBlockPrefab;
    public ForceBlock forceBlockPrefab;
    public WaitBlock waitBlockPrefab;

    [Header("UI Sprite Assets (Optional Overrides — Loaded from Assets/Textures/UI or Resources/LoopLab)")]
    public Sprite nodeCardFrameSprite;
    public Sprite ledPillSprite;
    public Sprite portSquareSprite;
    public Sprite portTriangleSprite;
    public Sprite labGridTileSprite;
    public Sprite playBtnSprite;
    public Sprite stopBtnSprite;
    public Sprite resetBtnSprite;

    // Internal state
    private readonly ProgramCommandList commands = new ProgramCommandList();
    private readonly List<CodeBlock> spawnedCommands = new List<CodeBlock>();
    [HideInInspector] public List<NodeCardUI> paletteCards = new List<NodeCardUI>();
    [HideInInspector] public List<NodeCardUI> graphNodes = new List<NodeCardUI>();

    // Built UI hierarchy references
    private Canvas mainCanvas;
    private RectTransform rootUiRect;
    private RectTransform topHudPanel;
    private TextMeshProUGUI levelTitleLabel;
    private TextMeshProUGUI objectiveHintLabel;
    private TextMeshProUGUI statusBannerLabel;
    private TextMeshProUGUI muteBtnLabel;
    private readonly List<Image> levelPillImages = new List<Image>();

    private RectTransform bottomSplitRoot;
    private RectTransform paletteContainer;
    private RectTransform labGridContainer;
    private RectTransform nodesLayer;
    private UILineConnector wireConnector;

    [HideInInspector] public NodeCardUI startNodeCard;
    [HideInInspector] public NodeCardUI endNodeCard;

    private Image centerPlayBtnIcon;
    private TextMeshProUGUI centerPlayStateLabel;
    private GameObject victoryOverlay;
    private TextMeshProUGUI victoryTitleText;
    private TextMeshProUGUI victorySubText;
    private Button victoryNextButton;

    // Interactive port wiring state
    private NodePortUI activeDragPort;
    private NodePortUI selectedClickPort;
    private int highlightedExecutionNodeIndex = -1;

    public int CommandCount
    {
        get
        {
            SyncOrderedCommandsFromGraph();
            return commands.Count;
        }
    }

    public bool IsEditingAllowed => codeExecutor == null || !codeExecutor.IsRunning();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadDefaultSpritesIfMissing();
    }

    private void Start()
    {
        if (codeExecutor == null) codeExecutor = FindFirstObjectByType<CodeExecutor>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        SubscribeExecutorEvents();
        if (rootUiRect == null)
        {
            ConfigureFixedProgramFrame();
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.EnsureCampaignInitialized();
                ConfigureForLevel(
                    LevelManager.Instance.CurrentLevel,
                    LevelManager.Instance.currentLevelIndex,
                    LevelManager.Instance.campaignLevels.Count);
            }
        }
    }

    private void OnDestroy()
    {
        UnsubscribeExecutorEvents();
        if (Instance == this) Instance = null;
    }

    private void SubscribeExecutorEvents()
    {
        if (codeExecutor == null) return;
        codeExecutor.OnStepStarted -= HandleStepStarted;
        codeExecutor.OnStepStarted += HandleStepStarted;
        codeExecutor.OnExecutionEnded -= HandleExecutionEnded;
        codeExecutor.OnExecutionEnded += HandleExecutionEnded;
        codeExecutor.OnStatusMessage -= HandleStatusMessage;
        codeExecutor.OnStatusMessage += HandleStatusMessage;
    }

    private void UnsubscribeExecutorEvents()
    {
        if (codeExecutor == null) return;
        codeExecutor.OnStepStarted -= HandleStepStarted;
        codeExecutor.OnExecutionEnded -= HandleExecutionEnded;
        codeExecutor.OnStatusMessage -= HandleStatusMessage;
    }

    public void LoadDefaultSpritesIfMissing()
    {
        if (nodeCardFrameSprite == null) nodeCardFrameSprite = LoadUiSprite("NodeCardFrame");
        if (ledPillSprite == null) ledPillSprite = LoadUiSprite("LedPill");
        if (portSquareSprite == null) portSquareSprite = LoadUiSprite("PortSquare");
        if (portTriangleSprite == null) portTriangleSprite = LoadUiSprite("PortTriangle");
        if (labGridTileSprite == null) labGridTileSprite = LoadUiSprite("LabGridTile");
        if (playBtnSprite == null) playBtnSprite = LoadUiSprite("Icon_PlayBtn");
        if (stopBtnSprite == null) stopBtnSprite = LoadUiSprite("Icon_StopBtn");
        if (resetBtnSprite == null) resetBtnSprite = LoadUiSprite("Icon_ResetBtn");
    }

    public static Sprite LoadUiSprite(string name)
    {
        Sprite s = Resources.Load<Sprite>("LoopLab/" + name);
        if (s != null) return s;

        Texture2D tex = Resources.Load<Texture2D>("LoopLab/" + name);
#if UNITY_EDITOR
        if (tex == null)
        {
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Textures/UI/{name}.png");
            if (s != null) return s;
            tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/UI/{name}.png");
        }
#endif
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return null;
    }

    // Entry point preserved for compatibility and Edit-Mode scene setup.
    public void ConfigureFixedProgramFrame()
    {
        LoadDefaultSpritesIfMissing();
        EnsureSplitScreenNodeLabUI();
    }

    public void EnsureSplitScreenNodeLabUI()
    {
        // Locate or create the main Canvas
        if (mainCanvas == null)
        {
            if (programPanel != null) mainCanvas = programPanel.GetComponentInParent<Canvas>(true);
            if (mainCanvas == null) mainCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        }

        if (mainCanvas == null)
        {
            var canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            mainCanvas = canvasObj.GetComponent<Canvas>();
        }

        mainCanvas.gameObject.SetActive(true);
        mainCanvas.enabled = true;
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = mainCanvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
            mainCanvas.gameObject.AddComponent<GraphicRaycaster>();

        // Hide legacy PalettePanel / SequencePanel objects cleanly without breaking references
        HideLegacyPanels(mainCanvas.transform);

        // Rebuild or bind "LoopLabRootUI"
        Transform existingRoot = mainCanvas.transform.Find("LoopLabRootUI");
        if (existingRoot != null)
        {
            if (Application.isPlaying) DestroyImmediate(existingRoot.gameObject);
            else DestroyImmediate(existingRoot.gameObject);
        }

        GameObject rootObj = CreateUiObject("LoopLabRootUI", mainCanvas.transform);
        rootUiRect = rootObj.GetComponent<RectTransform>();
        StretchFull(rootUiRect);

        BuildTopSceneOverlay(rootUiRect);
        BuildBottomNodeEditor(rootUiRect);
        BuildCenterDividerControls(rootUiRect);
        BuildVictoryModal(rootUiRect);
    }

    private void HideLegacyPanels(Transform canvasTransform)
    {
        string[] legacyNames = { "PalettePanel", "SequencePanel" };
        foreach (string n in legacyNames)
        {
            Transform t = canvasTransform.Find(n);
            if (t != null) t.gameObject.SetActive(false);
        }
        foreach (var slot in sequenceSlots)
        {
            if (slot != null) slot.gameObject.SetActive(false);
        }
    }

    private void BuildTopSceneOverlay(RectTransform parent)
    {
        GameObject topObj = CreateUiObject("TopSceneOverlay", parent);
        topHudPanel = topObj.GetComponent<RectTransform>();
        topHudPanel.anchorMin = new Vector2(0f, 0.48f);
        topHudPanel.anchorMax = new Vector2(1f, 1f);
        topHudPanel.offsetMin = Vector2.zero;
        topHudPanel.offsetMax = Vector2.zero;

        // Top-Left Title Banner
        GameObject titleBg = CreateUiObject("HeaderBanner", topHudPanel);
        var titleBgRect = titleBg.GetComponent<RectTransform>();
        titleBgRect.anchorMin = new Vector2(0f, 1f);
        titleBgRect.anchorMax = new Vector2(0f, 1f);
        titleBgRect.pivot = new Vector2(0f, 1f);
        titleBgRect.anchoredPosition = new Vector2(22f, -14f);
        titleBgRect.sizeDelta = new Vector2(820f, 68f);

        levelTitleLabel = CreateStyledText(
            titleBgRect,
            "LevelTitle",
            "PROTOCOL 01 // CLOSED LOOP",
            27f,
            new Color(0.00f, 0.90f, 1.00f, 1f),
            TextAlignmentOptions.TopLeft);
        levelTitleLabel.rectTransform.offsetMin = new Vector2(0f, 24f);
        levelTitleLabel.outlineWidth = 0.28f;
        levelTitleLabel.outlineColor = new Color32(6, 10, 18, 255);

        objectiveHintLabel = CreateStyledText(
            titleBgRect,
            "ObjectiveHint",
            "DRAG [WALK] TO THE CIRCUIT BOARD, THEN DRAG PORTS TO WIRE: START -> WALK -> LOOP",
            14.5f,
            new Color(0.86f, 0.92f, 0.98f, 0.94f),
            TextAlignmentOptions.BottomLeft);
        objectiveHintLabel.rectTransform.offsetMax = new Vector2(0f, -34f);

        // Top-Right Level Selector Bar (P1..P7 + Mute)
        GameObject rightBar = CreateUiObject("TopRightControls", topHudPanel);
        var rbRect = rightBar.GetComponent<RectTransform>();
        rbRect.anchorMin = new Vector2(1f, 1f);
        rbRect.anchorMax = new Vector2(1f, 1f);
        rbRect.pivot = new Vector2(1f, 1f);
        rbRect.anchoredPosition = new Vector2(-20f, -14f);
        rbRect.sizeDelta = new Vector2(456f, 40f);

        levelPillImages.Clear();
        for (int i = 0; i < 7; i++)
        {
            int levelIdx = i;
            GameObject pill = CreateUiObject($"LevelBtn_{i + 1}", rbRect, true, true);
            var pr = pill.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 0f);
            pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 0.5f);
            pr.anchoredPosition = new Vector2(i * 46f, 0f);
            pr.sizeDelta = new Vector2(40f, -4f);

            var img = pill.GetComponent<Image>();
            img.color = i == 0 ? new Color(0.00f, 0.72f, 0.92f, 0.96f) : new Color(0.10f, 0.14f, 0.22f, 0.90f);
            levelPillImages.Add(img);

            CreateStyledText(pr, "Lbl", $"P{i + 1}", 14f, Color.white, TextAlignmentOptions.Center);
            var btn = pill.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                if (LevelManager.Instance != null)
                    LevelManager.Instance.LoadCampaignLevel(levelIdx, true);
            });
        }

        // Mute / Audio Button
        GameObject muteBtnObj = CreateUiObject("MuteButton", rbRect, true, true);
        var mr = muteBtnObj.GetComponent<RectTransform>();
        mr.anchorMin = new Vector2(1f, 0f);
        mr.anchorMax = new Vector2(1f, 1f);
        mr.pivot = new Vector2(1f, 0.5f);
        mr.anchoredPosition = Vector2.zero;
        mr.sizeDelta = new Vector2(118f, -4f);
        var muteImg = muteBtnObj.GetComponent<Image>();
        muteImg.color = new Color(0.10f, 0.14f, 0.22f, 0.92f);
        muteBtnLabel = CreateStyledText(mr, "MuteLbl", "AUDIO: ON", 13.5f, new Color(0.55f, 0.92f, 1f), TextAlignmentOptions.Center);
        var muteBtn = muteBtnObj.GetComponent<Button>();
        muteBtn.targetGraphic = muteImg;
        muteBtn.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ToggleMute();
                muteBtnLabel.text = AudioManager.Instance.isMuted ? "AUDIO: OFF" : "AUDIO: ON";
            }
        });
    }

    private void BuildBottomNodeEditor(RectTransform parent)
    {
        GameObject bottomObj = CreateUiObject("BottomNodeWorkspace", parent, true);
        bottomSplitRoot = bottomObj.GetComponent<RectTransform>();
        bottomSplitRoot.anchorMin = new Vector2(0f, 0f);
        bottomSplitRoot.anchorMax = new Vector2(1f, 0.48f);
        bottomSplitRoot.offsetMin = Vector2.zero;
        bottomSplitRoot.offsetMax = Vector2.zero;
        bottomObj.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.11f, 1f);

        // =================================================================
        // 1. Bottom-Left: COMMAND MODULES Dock (X: 0.0 to 0.22)
        // =================================================================
        GameObject leftPanel = CreateUiObject("NodeModifiersPanel", bottomSplitRoot, true);
        var leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0.22f, 1f);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;
        leftPanel.GetComponent<Image>().color = new Color(0.07f, 0.10f, 0.16f, 1f);

        // Right-edge glowing cyan divider rail separating dock from circuit board
        GameObject dockRail = CreateUiObject("DockDividerRail", leftRect, true);
        var drRect = dockRail.GetComponent<RectTransform>();
        drRect.anchorMin = new Vector2(1f, 0f);
        drRect.anchorMax = new Vector2(1f, 1f);
        drRect.pivot = new Vector2(1f, 0.5f);
        drRect.anchoredPosition = Vector2.zero;
        drRect.sizeDelta = new Vector2(3f, 0f);
        dockRail.GetComponent<Image>().color = new Color(0.00f, 0.78f, 1.00f, 0.45f);

        // Top header bar inside COMMAND MODULES
        GameObject modHeader = CreateUiObject("ModifiersHeaderBar", leftRect, true);
        var mhRect = modHeader.GetComponent<RectTransform>();
        mhRect.anchorMin = new Vector2(0f, 1f);
        mhRect.anchorMax = new Vector2(1f, 1f);
        mhRect.pivot = new Vector2(0.5f, 1f);
        mhRect.anchoredPosition = Vector2.zero;
        mhRect.sizeDelta = new Vector2(-3f, 58f);
        modHeader.GetComponent<Image>().color = new Color(0.10f, 0.15f, 0.24f, 1f);

        var modTitle = CreateStyledText(
            mhRect,
            "ModifiersTitle",
            "COMMAND MODULES",
            21f,
            new Color(0.00f, 0.90f, 1.00f, 1f),
            TextAlignmentOptions.Center);
        modTitle.outlineWidth = 0.24f;
        modTitle.outlineColor = new Color32(6, 10, 18, 255);

        // Container for draggable palette cards
        GameObject paletteArea = CreateUiObject("PaletteCardsArea", leftRect);
        paletteContainer = paletteArea.GetComponent<RectTransform>();
        paletteContainer.anchorMin = new Vector2(0f, 0f);
        paletteContainer.anchorMax = new Vector2(1f, 1f);
        paletteContainer.offsetMin = new Vector2(12f, 44f);
        paletteContainer.offsetMax = new Vector2(-14f, -66f);

        // Helper footer at bottom of COMMAND MODULES
        var footerHint = CreateStyledText(
            leftRect,
            "PaletteFooterHint",
            "1. DRAG MODULE TO BOARD\n2. DRAG PORTS TO WIRE",
            12f,
            new Color(0.62f, 0.76f, 0.90f, 0.88f),
            TextAlignmentOptions.Center);
        footerHint.rectTransform.anchorMin = new Vector2(0f, 0f);
        footerHint.rectTransform.anchorMax = new Vector2(1f, 0f);
        footerHint.rectTransform.pivot = new Vector2(0.5f, 0f);
        footerHint.rectTransform.anchoredPosition = new Vector2(0f, 8f);
        footerHint.rectTransform.sizeDelta = new Vector2(-16f, 36f);

        // =================================================================
        // 2. Bottom-Right: LOOP CIRCUIT BOARD (X: 0.22 to 1.0)
        // =================================================================
        GameObject rightPanel = CreateUiObject("MachineLearningLabPanel", bottomSplitRoot, true);
        labGridContainer = rightPanel.GetComponent<RectTransform>();
        labGridContainer.anchorMin = new Vector2(0.22f, 0f);
        labGridContainer.anchorMax = new Vector2(1f, 1f);
        labGridContainer.offsetMin = Vector2.zero;
        labGridContainer.offsetMax = Vector2.zero;

        var gridImg = rightPanel.GetComponent<Image>();
        if (labGridTileSprite != null)
        {
            gridImg.sprite = labGridTileSprite;
            gridImg.type = Image.Type.Tiled;
            gridImg.pixelsPerUnitMultiplier = 1f;
            gridImg.color = Color.white;
        }
        else
        {
            gridImg.color = new Color(0.05f, 0.07f, 0.11f, 1f);
        }

        // Top-Right Title ("LOOP CIRCUIT BOARD")
        var labTitle = CreateStyledText(
            labGridContainer,
            "LabHeaderTitle",
            "LOOP CIRCUIT BOARD",
            22f,
            new Color(0.92f, 0.96f, 1.00f, 1f),
            TextAlignmentOptions.TopRight);
        labTitle.rectTransform.anchorMin = new Vector2(1f, 1f);
        labTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
        labTitle.rectTransform.pivot = new Vector2(1f, 1f);
        labTitle.rectTransform.anchoredPosition = new Vector2(-26f, -14f);
        labTitle.rectTransform.sizeDelta = new Vector2(460f, 34f);
        labTitle.outlineWidth = 0.24f;
        labTitle.outlineColor = new Color32(6, 10, 18, 255);

        // Live circuit status readout under LOOP CIRCUIT BOARD
        statusBannerLabel = CreateStyledText(
            labGridContainer,
            "StatusReadout",
            "CIRCUIT OPEN — DRAG MODULES TO BOARD & WIRE [START] -> MODULES -> [LOOP]",
            13.5f,
            new Color(0.98f, 0.72f, 0.24f, 0.95f),
            TextAlignmentOptions.TopRight);
        statusBannerLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
        statusBannerLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        statusBannerLabel.rectTransform.pivot = new Vector2(1f, 1f);
        statusBannerLabel.rectTransform.anchoredPosition = new Vector2(-26f, -44f);
        statusBannerLabel.rectTransform.sizeDelta = new Vector2(720f, 26f);

        // Wire Rendering Layer (behind nodes so wires connect cleanly at coaxial side ports)
        GameObject wireLayerObj = CreateUiObject("WireConnectionLayer", labGridContainer);
        var wireRect = wireLayerObj.GetComponent<RectTransform>();
        StretchFull(wireRect);
        wireConnector = wireLayerObj.AddComponent<UILineConnector>();
        wireConnector.raycastTarget = false;

        // Nodes Layer
        GameObject nodesLayerObj = CreateUiObject("GraphNodesLayer", labGridContainer);
        nodesLayer = nodesLayerObj.GetComponent<RectTransform>();
        StretchFull(nodesLayer);

        // Create Fixed START Emitter (Left) and Fixed LOOP Terminal (Right)
        startNodeCard = CreateNodeCardObject(BlockType.Start, nodesLayer, false, true);
        startNodeCard.rectTransform.anchoredPosition = new Vector2(-460f, -20f);

        endNodeCard = CreateNodeCardObject(BlockType.Stop, nodesLayer, false, true);
        endNodeCard.rectTransform.anchoredPosition = new Vector2(470f, -20f);
    }

    private void BuildCenterDividerControls(RectTransform parent)
    {
        // Horizontal dark divider strip at Y = 0.48 with glowing cyan horizon line
        GameObject dividerLine = CreateUiObject("CenterDividerStrip", parent, true);
        var dlRect = dividerLine.GetComponent<RectTransform>();
        dlRect.anchorMin = new Vector2(0f, 0.48f);
        dlRect.anchorMax = new Vector2(1f, 0.48f);
        dlRect.pivot = new Vector2(0.5f, 0.5f);
        dlRect.anchoredPosition = Vector2.zero;
        dlRect.sizeDelta = new Vector2(0f, 6f);
        dividerLine.GetComponent<Image>().color = new Color(0.00f, 0.78f, 1.00f, 0.55f);

        // Center Play / Stop circular button straddling the divider
        GameObject playBtnObj = CreateUiObject("CenterPlayButton", parent, true, true);
        var playRect = playBtnObj.GetComponent<RectTransform>();
        playRect.anchorMin = new Vector2(0.5f, 0.48f);
        playRect.anchorMax = new Vector2(0.5f, 0.48f);
        playRect.pivot = new Vector2(0.5f, 0.5f);
        playRect.anchoredPosition = new Vector2(0f, 0f);
        playRect.sizeDelta = new Vector2(58f, 58f);

        centerPlayBtnIcon = playBtnObj.GetComponent<Image>();
        if (playBtnSprite != null) centerPlayBtnIcon.sprite = playBtnSprite;
        centerPlayBtnIcon.color = Color.white;

        var playBtn = playBtnObj.GetComponent<Button>();
        playBtn.targetGraphic = centerPlayBtnIcon;
        playBtn.onClick.AddListener(OnCenterPlayStopClicked);

        centerPlayStateLabel = CreateStyledText(
            playRect,
            "PlayHint",
            "RUN LOOP",
            11.5f,
            new Color(0.75f, 0.94f, 1.00f, 0.95f),
            TextAlignmentOptions.Center);
        centerPlayStateLabel.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        centerPlayStateLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        centerPlayStateLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
        centerPlayStateLabel.rectTransform.anchoredPosition = new Vector2(0f, -2f);
        centerPlayStateLabel.rectTransform.sizeDelta = new Vector2(110f, 18f);

        // Reset Robot button on the left of Play
        CreateDividerUtilityButton(parent, "ResetRobotBtn", new Vector2(-92f, 0f), new Vector2(102f, 34f),
            "RESET", new Color(0.12f, 0.18f, 0.28f, 0.96f), ResetProgram);

        // Clear Board button on the right of Play (no Auto-Wire button so player wires manually!)
        CreateDividerUtilityButton(parent, "ClearGraphBtn", new Vector2(92f, 0f), new Vector2(102f, 34f),
            "CLEAR", new Color(0.32f, 0.12f, 0.18f, 0.96f), ClearAllSlots);
    }

    private void CreateDividerUtilityButton(
        RectTransform parent,
        string name,
        Vector2 offsetFromDividerCenter,
        Vector2 size,
        string caption,
        Color bgColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUiObject(name, parent, true, true);
        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.48f);
        rt.anchorMax = new Vector2(0.5f, 0.48f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offsetFromDividerCenter;
        rt.sizeDelta = size;

        var img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        CreateStyledText(rt, "Label", caption, 13f, Color.white, TextAlignmentOptions.Center);

        var btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
    }

    private void BuildVictoryModal(RectTransform parent)
    {
        victoryOverlay = CreateUiObject("VictoryBannerOverlay", parent, true);
        var vr = victoryOverlay.GetComponent<RectTransform>();
        vr.anchorMin = new Vector2(0.5f, 0.74f);
        vr.anchorMax = new Vector2(0.5f, 0.74f);
        vr.pivot = new Vector2(0.5f, 0.5f);
        vr.anchoredPosition = Vector2.zero;
        vr.sizeDelta = new Vector2(660f, 175f);
        victoryOverlay.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.96f);

        // Neon green top accent bar
        GameObject topStripe = CreateUiObject("TopAccent", vr, true);
        var tsRect = topStripe.GetComponent<RectTransform>();
        tsRect.anchorMin = new Vector2(0f, 1f);
        tsRect.anchorMax = new Vector2(1f, 1f);
        tsRect.pivot = new Vector2(0.5f, 1f);
        tsRect.anchoredPosition = Vector2.zero;
        tsRect.sizeDelta = new Vector2(0f, 5f);
        topStripe.GetComponent<Image>().color = new Color(0.28f, 0.96f, 0.48f, 1f);

        victoryTitleText = CreateStyledText(
            vr,
            "VictoryTitle",
            "DEPLOYMENT TEST CLEARED!",
            24f,
            new Color(0.32f, 0.98f, 0.52f, 1f),
            TextAlignmentOptions.Center);
        victoryTitleText.rectTransform.anchoredPosition = new Vector2(0f, 36f);
        victoryTitleText.rectTransform.sizeDelta = new Vector2(620f, 38f);

        victorySubText = CreateStyledText(
            vr,
            "VictorySub",
            "LOOP SEQUENCE VERIFIED — NEW NODE MODIFIER UNLOCKED",
            14.5f,
            new Color(0.84f, 0.88f, 0.94f, 1f),
            TextAlignmentOptions.Center);
        victorySubText.rectTransform.anchoredPosition = new Vector2(0f, 2f);
        victorySubText.rectTransform.sizeDelta = new Vector2(620f, 28f);

        GameObject nextBtnObj = CreateUiObject("NextDeploymentBtn", vr, true, true);
        var nbr = nextBtnObj.GetComponent<RectTransform>();
        nbr.anchorMin = new Vector2(0.5f, 0f);
        nbr.anchorMax = new Vector2(0.5f, 0f);
        nbr.pivot = new Vector2(0.5f, 0f);
        nbr.anchoredPosition = new Vector2(0f, 16f);
        nbr.sizeDelta = new Vector2(240f, 44f);
        var nbImg = nextBtnObj.GetComponent<Image>();
        nbImg.color = new Color(0.18f, 0.68f, 0.36f, 1f);

        CreateStyledText(nbr, "NextLabel", "NEXT TEST LEVEL >>", 16f, Color.white, TextAlignmentOptions.Center);

        victoryNextButton = nextBtnObj.GetComponent<Button>();
        victoryNextButton.targetGraphic = nbImg;
        victoryNextButton.onClick.AddListener(() =>
        {
            HideVictoryBanner();
            if (LevelManager.Instance != null)
                LevelManager.Instance.LoadNextLevel();
        });

        victoryOverlay.SetActive(false);
    }

    // Configures the UI for a specific LevelDefinition (Level 1..7)
    public void ConfigureForLevel(LevelDefinition level, int levelIdx, int totalLevels)
    {
        if (rootUiRect == null || levelPillImages.Count != 7) EnsureSplitScreenNodeLabUI();
        HideVictoryBanner();

        if (levelTitleLabel != null)
            levelTitleLabel.text = level != null ? level.title : $"PROTOCOL 0{levelIdx + 1}";
        if (objectiveHintLabel != null)
            objectiveHintLabel.text = level != null ? level.objectiveHint : "";

        for (int i = 0; i < levelPillImages.Count; i++)
        {
            if (levelPillImages[i] != null)
            {
                levelPillImages[i].color = (i == levelIdx)
                    ? new Color(0.00f, 0.72f, 0.92f, 0.98f)
                    : new Color(0.10f, 0.14f, 0.22f, 0.90f);
            }
        }

        // Clear existing placed graph nodes and palette cards
        foreach (var gn in graphNodes)
        {
            if (gn != null) DestroyImmediate(gn.gameObject);
        }
        graphNodes.Clear();

        foreach (var pc in paletteCards)
        {
            if (pc != null) DestroyImmediate(pc.gameObject);
        }
        paletteCards.Clear();

        if (startNodeCard != null) startNodeCard.connectedNext = null;
        if (endNodeCard != null) endNodeCard.connectedPrev = null;

        // Populate COMMAND MODULES palette in a 2-column grid
        if (level != null && paletteContainer != null)
        {
            int slotIndex = 0;
            foreach (var item in level.unlockedNodes)
            {
                int count = Mathf.Max(1, item.count);
                for (int c = 0; c < count; c++)
                {
                    NodeCardUI card = CreateNodeCardObject(item.nodeType, paletteContainer, true, false);
                    card.paletteSlotIndex = slotIndex;
                    paletteCards.Add(card);
                    slotIndex++;
                }
            }
            ReflowPaletteCards();
        }

        commands.Clear();
        RefreshWires();
        UpdatePlayButtonVisual(false);
        SetStatusBanner("CIRCUIT OPEN — DRAG MODULES TO BOARD & DRAG PORTS TO WIRE [START] -> [LOOP]");
    }

    private void PositionPaletteCard(RectTransform rt, int index, int totalVisible = 4)
    {
        int col = index % 2;
        int row = index / 2;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(110f, 110f);

        if (totalVisible > 4)
        {
            rt.localScale = new Vector3(0.84f, 0.84f, 1f);
            float x = (col == 0) ? -64f : 64f;
            float y = -6f - row * 100f;
            rt.anchoredPosition = new Vector2(x, y);
        }
        else
        {
            rt.localScale = Vector3.one;
            float x = (col == 0) ? -68f : 68f;
            float y = -14f - row * 122f;
            rt.anchoredPosition = new Vector2(x, y);
        }
    }

    private void ReflowPaletteCards()
    {
        int totalVisible = 0;
        foreach (var pc in paletteCards)
        {
            if (pc != null && pc.gameObject.activeSelf)
                totalVisible++;
        }

        int visibleIdx = 0;
        foreach (var pc in paletteCards)
        {
            if (pc != null && pc.gameObject.activeSelf)
            {
                PositionPaletteCard(pc.rectTransform, visibleIdx, totalVisible);
                visibleIdx++;
            }
        }
    }

    private NodeCardUI CreateNodeCardObject(BlockType type, RectTransform parent, bool isPalette, bool isEndpoint)
    {
        GameObject cardObj = CreateUiObject($"Node_{CommandLabels.For(type)}", parent, true);
        var rt = cardObj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(110f, 110f);

        var card = cardObj.AddComponent<NodeCardUI>();
        card.rectTransform = rt;
        card.blockType = type;
        card.isPaletteCard = isPalette;
        card.isFixedEndpoint = isEndpoint;

        Color accentCol = CommandLabels.LedColorFor(type);

        // Highlight border when executing
        GameObject hlObj = CreateUiObject("HighlightGlow", rt, true);
        var hlRect = hlObj.GetComponent<RectTransform>();
        StretchFull(hlRect);
        hlRect.offsetMin = new Vector2(-6f, -6f);
        hlRect.offsetMax = new Vector2(6f, 6f);
        hlObj.transform.SetAsFirstSibling();
        var hlImg = hlObj.GetComponent<Image>();
        hlImg.color = new Color(0.35f, 1.0f, 0.72f, 0.90f);
        hlImg.raycastTarget = false;
        hlImg.enabled = false;
        card.highlightBorder = hlImg;

        // Card chassis background (NodeCardFrame.png)
        var bgImg = cardObj.GetComponent<Image>();
        if (nodeCardFrameSprite != null) bgImg.sprite = nodeCardFrameSprite;
        bgImg.color = Color.white;
        card.cardBackground = bgImg;

        // Top-Left Glowing Telemetry LED Pill inside Header Bar
        GameObject ledObj = CreateUiObject("HeaderLed", rt, true);
        var ledRect = ledObj.GetComponent<RectTransform>();
        ledRect.anchorMin = ledRect.anchorMax = new Vector2(0f, 1f);
        ledRect.pivot = new Vector2(0f, 0.5f);
        ledRect.anchoredPosition = new Vector2(14f, -17f);
        ledRect.sizeDelta = new Vector2(22f, 8f);
        var ledImg = ledObj.GetComponent<Image>();
        if (ledPillSprite != null) ledImg.sprite = ledPillSprite;
        ledImg.color = accentCol;
        ledImg.raycastTarget = false;
        card.ledIndicator = ledImg;

        // Top-Right Module Name Label ("START", "WALK", "JUMP", "LEFT", "RIGHT", "LOOP")
        var titleTxt = CreateStyledText(
            rt,
            "NodeTitle",
            CommandLabels.For(type),
            12.5f,
            accentCol,
            TextAlignmentOptions.Right);
        titleTxt.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleTxt.rectTransform.anchoredPosition = new Vector2(-2f, -9f);
        titleTxt.rectTransform.sizeDelta = new Vector2(-26f, 18f);
        card.titleLabel = titleTxt;

        // Center Recessed Action Glyph
        GameObject iconObj = CreateUiObject("ActionIcon", rt, true);
        var iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(0f, -2f);
        iconRect.sizeDelta = new Vector2(52f, 52f);
        var iconImg = iconObj.GetComponent<Image>();
        Sprite iconSprite = LoadUiSprite(CommandLabels.IconNameFor(type));
        if (iconSprite != null) iconImg.sprite = iconSprite;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
        card.actionIcon = iconImg;

        // Bottom Telemetry Sub-Badge ("+1 TILE", "SKIP 1", "SIDE L", "SIDE R", "EMITTER", "REPEAT")
        var subBadgeTxt = CreateStyledText(
            rt,
            "SubBadge",
            CommandLabels.SubBadgeFor(type),
            9.5f,
            new Color(0.76f, 0.86f, 0.96f, 0.90f),
            TextAlignmentOptions.Center);
        subBadgeTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
        subBadgeTxt.rectTransform.anchorMax = new Vector2(1f, 0f);
        subBadgeTxt.rectTransform.pivot = new Vector2(0.5f, 0f);
        subBadgeTxt.rectTransform.anchoredPosition = new Vector2(0f, 7f);
        subBadgeTxt.rectTransform.sizeDelta = new Vector2(-20f, 14f);

        // Coaxial Input & Output Ports are only active once a module is on the LOOP CIRCUIT BOARD (!isPalette)
        if (!isPalette && type != BlockType.Start)
        {
            GameObject inPortObj = CreateUiObject("InputPort", rt, true);
            var inRect = inPortObj.GetComponent<RectTransform>();
            inRect.anchorMin = inRect.anchorMax = new Vector2(0f, 0.5f);
            inRect.pivot = new Vector2(0.5f, 0.5f);
            inRect.anchoredPosition = new Vector2(-4f, -2f);
            inRect.sizeDelta = new Vector2(26f, 26f);

            var inImg = inPortObj.GetComponent<Image>();
            if (portSquareSprite != null) inImg.sprite = portSquareSprite;
            inImg.color = new Color(0.42f, 0.62f, 0.82f, 0.95f);
            var portComp = inPortObj.AddComponent<NodePortUI>();
            portComp.ownerNode = card;
            portComp.isOutputPort = false;

            card.inputPortRect = inRect;
            card.inputPortImage = inImg;
        }

        if (!isPalette && type != BlockType.Stop)
        {
            GameObject outPortObj = CreateUiObject("OutputPort", rt, true);
            var outRect = outPortObj.GetComponent<RectTransform>();
            outRect.anchorMin = outRect.anchorMax = new Vector2(1f, 0.5f);
            outRect.pivot = new Vector2(0.5f, 0.5f);
            outRect.anchoredPosition = new Vector2(4f, -2f);
            outRect.sizeDelta = new Vector2(26f, 26f);

            var outImg = outPortObj.GetComponent<Image>();
            Sprite pSprite = portTriangleSprite != null ? portTriangleSprite : portSquareSprite;
            if (pSprite != null) outImg.sprite = pSprite;
            outImg.color = accentCol;

            var portComp = outPortObj.AddComponent<NodePortUI>();
            portComp.ownerNode = card;
            portComp.isOutputPort = true;

            card.outputPortRect = outRect;
            card.outputPortImage = outImg;
        }

        return card;
    }

    // =====================================================================
    // Manual Drag & Drop Placement & Manual Port Wiring Logic
    // =====================================================================

    public void TryDropPaletteNodeOnLab(NodeCardUI paletteCard, Vector2 screenPos)
    {
        if (!IsEditingAllowed || paletteCard == null || labGridContainer == null) return;

        Camera cam = mainCanvas != null && mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? mainCanvas.worldCamera
            : null;

        if (!RectTransformUtility.RectangleContainsScreenPoint(labGridContainer, screenPos, cam))
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(nodesLayer, screenPos, cam, out Vector2 localPt))
        {
            SpawnGraphNodeFromPalette(paletteCard, SnapToGrid(localPt));
        }
    }

    public void QuickPlaceFromPalette(NodeCardUI paletteCard)
    {
        if (!IsEditingAllowed || paletteCard == null || nodesLayer == null) return;

        int idx = graphNodes.Count;
        bool fivePlus = paletteCards != null && paletteCards.Count > 4;
        float x = fivePlus ? (-280f + idx * 140f) : (-220f + idx * 160f);
        float y = (idx % 2 == 0) ? 28f : -48f;
        Vector2 pos = SnapToGrid(new Vector2(Mathf.Clamp(x, -310f, 310f), y));

        // Places node UNWIRED on the circuit board — player manually wires the ports!
        SpawnGraphNodeFromPalette(paletteCard, pos);
    }

    private NodeCardUI SpawnGraphNodeFromPalette(NodeCardUI paletteCard, Vector2 localLabPos)
    {
        // Move card out of COMMAND MODULES dock onto the LOOP CIRCUIT BOARD unwired!
        paletteCard.gameObject.SetActive(false);
        ReflowPaletteCards();

        NodeCardUI node = CreateNodeCardObject(paletteCard.blockType, nodesLayer, false, false);
        node.paletteSlotIndex = paletteCard.paletteSlotIndex;
        node.rectTransform.anchoredPosition = localLabPos;
        graphNodes.Add(node);

        AudioManager.Instance?.PlayNodePlace();

        // Intentionally DO NOT auto-wire! Player drags from Output Port to Input Port themselves.
        RefreshWires();
        SetStatusBanner($"MODULE [{CommandLabels.For(node.blockType)}] PLACED — DRAG PORTS TO WIRE YOUR LOOP!");
        return node;
    }

    public void OnGraphNodeDragEnded(NodeCardUI graphNode, Vector2 screenPos)
    {
        if (!IsEditingAllowed || graphNode == null || graphNode.isFixedEndpoint) return;

        Camera cam = mainCanvas != null && mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? mainCanvas.worldCamera
            : null;

        // If dragged back onto NODE MODIFIERS panel on the left, remove from graph and restore to palette
        if (paletteContainer != null && RectTransformUtility.RectangleContainsScreenPoint(
            paletteContainer.parent as RectTransform, screenPos, cam))
        {
            RemoveGraphNode(graphNode);
            return;
        }

        graphNode.rectTransform.anchoredPosition = SnapToGrid(graphNode.rectTransform.anchoredPosition);
        RefreshWires();
    }

    public void RemoveGraphNode(NodeCardUI graphNode)
    {
        if (!IsEditingAllowed || graphNode == null || graphNode.isFixedEndpoint) return;

        // Heal wire if this node was connected between prev and next
        NodeCardUI prev = graphNode.connectedPrev;
        NodeCardUI next = graphNode.connectedNext;

        if (prev != null) prev.connectedNext = null;
        if (next != null) next.connectedPrev = null;

        if (prev != null && next != null && !(prev == startNodeCard && next == endNodeCard))
        {
            ConnectNodes(prev, next);
        }

        graphNodes.Remove(graphNode);

        // Restore corresponding card in NODE MODIFIERS palette
        foreach (var pc in paletteCards)
        {
            if (pc != null && pc.paletteSlotIndex == graphNode.paletteSlotIndex && !pc.gameObject.activeSelf)
            {
                pc.gameObject.SetActive(true);
                break;
            }
        }
        ReflowPaletteCards();

        Destroy(graphNode.gameObject);
        AudioManager.Instance?.PlayNodePlace();
        RefreshWires();
    }

    public void OnNodeCardClicked(NodeCardUI card)
    {
        if (selectedClickPort != null && card != null)
        {
            if (selectedClickPort.isOutputPort && card.inputPortRect != null)
            {
                ConnectNodes(selectedClickPort.ownerNode, card);
                selectedClickPort = null;
                RefreshWires();
            }
            else if (!selectedClickPort.isOutputPort && card.outputPortRect != null)
            {
                ConnectNodes(card, selectedClickPort.ownerNode);
                selectedClickPort = null;
                RefreshWires();
            }
        }
    }

    // Port Drag-to-Wire
    public void BeginPortWireDrag(NodePortUI port, Vector2 screenPos)
    {
        if (!IsEditingAllowed || port == null) return;
        activeDragPort = port;
        UpdatePortWireDrag(screenPos);
    }

    public void UpdatePortWireDrag(Vector2 screenPos)
    {
        if (activeDragPort == null || wireConnector == null || nodesLayer == null) return;
        Camera cam = mainCanvas != null && mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? mainCanvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(nodesLayer, screenPos, cam, out Vector2 endLocal))
        {
            Vector2 startLocal = GetPortPositionInLab(activeDragPort.ownerNode, activeDragPort.isOutputPort);
            Color col = CommandLabels.WireColorFor(0, activeDragPort.ownerNode.blockType);
            wireConnector.SetPreviewWire(startLocal, endLocal, col);
        }
    }

    public void CompletePortWireDrop(NodePortUI targetPort)
    {
        if (activeDragPort == null || targetPort == null || activeDragPort == targetPort) return;
        if (activeDragPort.isOutputPort && !targetPort.isOutputPort)
        {
            ConnectNodes(activeDragPort.ownerNode, targetPort.ownerNode);
        }
        else if (!activeDragPort.isOutputPort && targetPort.isOutputPort)
        {
            ConnectNodes(targetPort.ownerNode, activeDragPort.ownerNode);
        }
        activeDragPort = null;
        wireConnector?.ClearPreviewWire();
        RefreshWires();
    }

    public void EndPortWireDrag(PointerEventData eventData)
    {
        if (activeDragPort != null && eventData != null)
        {
            // Raycast under pointer to allow dropping anywhere on a target NodeCardUI or Port!
            var results = new List<RaycastResult>();
            EventSystem.current?.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                var port = r.gameObject.GetComponentInParent<NodePortUI>();
                if (port != null && port != activeDragPort)
                {
                    CompletePortWireDrop(port);
                    return;
                }
                var card = r.gameObject.GetComponentInParent<NodeCardUI>();
                if (card != null && !card.isPaletteCard && card != activeDragPort.ownerNode)
                {
                    if (activeDragPort.isOutputPort && card.blockType != BlockType.Start)
                        ConnectNodes(activeDragPort.ownerNode, card);
                    else if (!activeDragPort.isOutputPort && card.blockType != BlockType.Stop)
                        ConnectNodes(card, activeDragPort.ownerNode);
                    break;
                }
            }
        }
        activeDragPort = null;
        wireConnector?.ClearPreviewWire();
        RefreshWires();
    }

    public void OnPortClicked(NodePortUI port)
    {
        if (!IsEditingAllowed || port == null) return;
        if (selectedClickPort == null)
        {
            selectedClickPort = port;
            SetStatusBanner($"SELECTED {CommandLabels.For(port.ownerNode.blockType)} PORT — CLICK TARGET PORT TO WIRE");
        }
        else
        {
            if (selectedClickPort != port)
            {
                if (selectedClickPort.isOutputPort && !port.isOutputPort)
                    ConnectNodes(selectedClickPort.ownerNode, port.ownerNode);
                else if (!selectedClickPort.isOutputPort && port.isOutputPort)
                    ConnectNodes(port.ownerNode, selectedClickPort.ownerNode);
            }
            selectedClickPort = null;
            RefreshWires();
        }
    }

    public void DisconnectPort(NodePortUI port)
    {
        if (!IsEditingAllowed || port == null || port.ownerNode == null) return;
        if (port.isOutputPort)
        {
            var next = port.ownerNode.connectedNext;
            if (next != null) next.connectedPrev = null;
            port.ownerNode.connectedNext = null;
        }
        else
        {
            var prev = port.ownerNode.connectedPrev;
            if (prev != null) prev.connectedNext = null;
            port.ownerNode.connectedPrev = null;
        }
        AudioManager.Instance?.PlayNodeConnect();
        RefreshWires();
    }

    public void ConnectNodes(NodeCardUI fromNode, NodeCardUI toNode)
    {
        if (fromNode == null || toNode == null || fromNode == toNode) return;
        if (fromNode.blockType == BlockType.Stop || toNode.blockType == BlockType.Start) return;
        if (fromNode == startNodeCard && toNode == endNodeCard) return;

        // Prevent cycles among middle nodes
        NodeCardUI walker = toNode;
        int guard = 0;
        while (walker != null && guard++ < 32)
        {
            if (walker == fromNode) return;
            walker = walker.connectedNext;
        }

        if (fromNode.connectedNext != null)
            fromNode.connectedNext.connectedPrev = null;
        if (toNode.connectedPrev != null)
            toNode.connectedPrev.connectedNext = null;

        fromNode.connectedNext = toNode;
        toNode.connectedPrev = fromNode;

        AudioManager.Instance?.PlayNodeConnect();
        RefreshWires();

        if (IsChainConnectedToEnd())
        {
            SetStatusBanner($"CIRCUIT ARMED // {GetWiredGraphNodes().Count} MODULE(S) WIRED IN LOOP — CLICK PLAY!");
        }
        else
        {
            SetStatusBanner("WIRE CONNECTED — CONTINUE WIRING FROM [START] ALL THE WAY TO [LOOP]");
        }
    }

    private void SpliceNodeAtEndOfChain(NodeCardUI newNode)
    {
        if (startNodeCard == null || endNodeCard == null || newNode == null) return;

        NodeCardUI tail = startNodeCard;
        int guard = 0;
        while (tail.connectedNext != null && tail.connectedNext != endNodeCard && guard++ < 32)
        {
            tail = tail.connectedNext;
        }

        ConnectNodes(tail, newNode);
        ConnectNodes(newNode, endNodeCard);
    }

    public void AutoWireAllGraphNodes()
    {
        if (!IsEditingAllowed || startNodeCard == null || endNodeCard == null) return;

        startNodeCard.connectedNext = null;
        endNodeCard.connectedPrev = null;
        foreach (var n in graphNodes)
        {
            if (n != null)
            {
                n.connectedPrev = null;
                n.connectedNext = null;
            }
        }

        var ordered = graphNodes
            .Where(n => n != null)
            .OrderBy(n => n.rectTransform.anchoredPosition.x)
            .ToList();

        if (ordered.Count == 0)
        {
            RefreshWires();
            SetStatusBanner("PLACE AT LEAST ONE MODULE ON THE BOARD FIRST!");
            return;
        }

        NodeCardUI prev = startNodeCard;
        foreach (var node in ordered)
        {
            prev.connectedNext = node;
            node.connectedPrev = prev;
            prev = node;
        }
        prev.connectedNext = endNodeCard;
        endNodeCard.connectedPrev = prev;

        AudioManager.Instance?.PlayNodeConnect();
        RefreshWires();
        SetStatusBanner($"CIRCUIT ARMED // {ordered.Count} MODULE(S) WIRED IN LOOP — CLICK PLAY!");
    }

    public void RefreshWires()
    {
        if (wireConnector == null || nodesLayer == null) return;

        var segments = new List<WireSegment>();
        bool circuitClosed = IsChainConnectedToEnd();
        Color intentionalWireColor = CommandLabels.IntentionalWireColor(circuitClosed);

        // Gather all directed edges starting from START and any graphNode
        var allSources = new List<NodeCardUI>();
        if (startNodeCard != null) allSources.Add(startNodeCard);
        allSources.AddRange(graphNodes.Where(n => n != null));

        foreach (var src in allSources)
        {
            NodeCardUI dst = src.connectedNext;
            if (dst == null) continue;

            Vector2 pOut = GetPortPositionInLab(src, true);
            Vector2 pIn = GetPortPositionInLab(dst, false);
            bool highlighted = IsEdgeHighlighted(src);

            // Single, intentional PCB trace wire per connection:
            // Standby Amber while circuit is open, Powered Cyber Cyan once START -> ... -> LOOP is closed!
            segments.Add(new WireSegment(pOut, pIn, intentionalWireColor, 4.6f, highlighted));
        }

        // Update input port socket glow to reflect whether a wire is plugged into each input port
        var allTargets = new List<NodeCardUI>(graphNodes.Where(n => n != null));
        if (endNodeCard != null) allTargets.Add(endNodeCard);
        foreach (var node in allTargets)
        {
            if (node.inputPortImage != null)
            {
                node.inputPortImage.color = (node.connectedPrev != null)
                    ? intentionalWireColor
                    : new Color(0.38f, 0.54f, 0.72f, 0.85f);
            }
        }

        if (statusBannerLabel != null)
        {
            statusBannerLabel.color = circuitClosed
                ? new Color(0.25f, 0.94f, 1.00f, 0.98f)
                : new Color(0.98f, 0.72f, 0.24f, 0.95f);
        }

        wireConnector.SetWires(segments);
        SyncOrderedCommandsFromGraph();
    }

    private bool IsEdgeHighlighted(NodeCardUI src)
    {
        if (highlightedExecutionNodeIndex < 0) return false;
        var chain = GetWiredGraphNodes();
        if (highlightedExecutionNodeIndex < chain.Count)
        {
            NodeCardUI activeNode = chain[highlightedExecutionNodeIndex];
            return src == activeNode || (highlightedExecutionNodeIndex == 0 && src == startNodeCard);
        }
        return false;
    }

    private Vector2 GetPortPositionInLab(NodeCardUI node, bool isOutput)
    {
        if (node == null || node.rectTransform == null) return Vector2.zero;
        Vector2 basePos = node.rectTransform.anchoredPosition;
        float halfW = node.rectTransform.sizeDelta.x * 0.5f;
        return basePos + new Vector2(isOutput ? halfW : -halfW, -2f);
    }

    private List<NodeCardUI> GetWiredGraphNodes()
    {
        var result = new List<NodeCardUI>();
        if (startNodeCard == null) return result;

        NodeCardUI curr = startNodeCard.connectedNext;
        int guard = 0;
        while (curr != null && curr != endNodeCard && guard++ < 32)
        {
            result.Add(curr);
            curr = curr.connectedNext;
        }
        return result;
    }

    private bool IsChainConnectedToEnd()
    {
        if (startNodeCard == null || endNodeCard == null) return false;
        NodeCardUI curr = startNodeCard.connectedNext;
        int count = 0;
        while (curr != null && count < 32)
        {
            if (curr == endNodeCard) return count > 0;
            count++;
            curr = curr.connectedNext;
        }
        return false;
    }

    private void SyncOrderedCommandsFromGraph()
    {
        if (!IsChainConnectedToEnd())
        {
            commands.Clear();
            return;
        }
        var wired = GetWiredGraphNodes();
        commands.SetCommands(wired.Select(n => n.blockType));
    }

    private static Vector2 SnapToGrid(Vector2 pos)
    {
        const float snap = 16f;
        return new Vector2(
            Mathf.Round(pos.x / snap) * snap,
            Mathf.Round(pos.y / snap) * snap);
    }

    // =====================================================================
    // Program Execution & Compatibility API
    // =====================================================================

    public bool TryAddBlock(BlockType type)
    {
        if (!IsEditingAllowed || ProgramSequence.IsBoundary(type)) return false;
        if (rootUiRect == null) EnsureSplitScreenNodeLabUI();

        foreach (var pc in paletteCards)
        {
            if (pc != null && pc.gameObject.activeSelf && pc.blockType == type)
            {
                QuickPlaceFromPalette(pc);
                return true;
            }
        }

        // Fallback if added programmatically beyond palette stock
        int idx = graphNodes.Count;
        Vector2 pos = SnapToGrid(new Vector2(-220f + idx * 175f, 0f));
        NodeCardUI node = CreateNodeCardObject(type, nodesLayer, false, false);
        node.rectTransform.anchoredPosition = pos;
        graphNodes.Add(node);
        RefreshWires();
        return true;
    }

    public void RemoveCommandAt(int index)
    {
        var wired = GetWiredGraphNodes();
        if (index >= 0 && index < wired.Count)
            RemoveGraphNode(wired[index]);
    }

    public void ClearAllSlots()
    {
        if (!IsEditingAllowed) StopProgram();

        for (int i = graphNodes.Count - 1; i >= 0; i--)
        {
            if (graphNodes[i] != null)
                Destroy(graphNodes[i].gameObject);
        }
        graphNodes.Clear();

        foreach (var pc in paletteCards)
        {
            if (pc != null) pc.gameObject.SetActive(true);
        }
        ReflowPaletteCards();

        if (startNodeCard != null) startNodeCard.connectedNext = null;
        if (endNodeCard != null) endNodeCard.connectedPrev = null;

        commands.Clear();
        RefreshWires();
        SetStatusBanner("BOARD CLEARED — DRAG MODULES FROM COMMAND MODULES & WIRE [START] -> [LOOP]");
    }

    private void OnCenterPlayStopClicked()
    {
        if (codeExecutor != null && codeExecutor.IsRunning())
        {
            StopProgram();
        }
        else
        {
            CollectBlocksAndRun();
        }
    }

    public void CollectBlocksAndRun()
    {
        if (codeExecutor == null) codeExecutor = FindFirstObjectByType<CodeExecutor>();
        if (codeExecutor == null || codeExecutor.IsRunning()) return;

        HideVictoryBanner();

        // Require manual circuit wiring from START through placed modules to LOOP!
        if (!IsChainConnectedToEnd())
        {
            SetStatusBanner("CIRCUIT OPEN! MANUALLY DRAG WIRES FROM [START] -> MODULES -> [LOOP] FIRST");
            AudioManager.Instance?.PlayBump();
            Debug.LogWarning("Circuit is not closed! Drag wires from START through your modules to LOOP.", this);
            return;
        }

        SyncOrderedCommandsFromGraph();
        if (commands.Count == 0)
        {
            SetStatusBanner("CIRCUIT OPEN! DRAG A MODULE TO THE BOARD & WIRE [START] -> MODULE -> [LOOP]");
            return;
        }

        // Always start loop cleanly from the StartTile
        if (player == null) player = codeExecutor.player;
        if (player != null && player.currentState == PlayerState.Win)
        {
            if (LevelManager.Instance != null) LevelManager.Instance.ResetForRetry();
        }

        var ordered = commands.BuildSequence();

        foreach (var old in spawnedCommands)
        {
            if (old != null) Destroy(old.gameObject);
        }
        spawnedCommands.Clear();

        foreach (var type in ordered)
        {
            CodeBlock block = InstantiateBlockForType(type);
            block.gameObject.hideFlags = HideFlags.HideInHierarchy;
            spawnedCommands.Add(block);
        }

        UpdatePlayButtonVisual(true);
        codeExecutor.Run(new List<CodeBlock>(spawnedCommands));
    }

    private CodeBlock InstantiateBlockForType(BlockType type)
    {
        CodeBlock prefab = GetPrefab(type);
        if (prefab != null)
        {
            return Instantiate(prefab, transform);
        }

        // Scalable fallback: create UniversalCodeBlock dynamically for any new BlockType!
        var go = new GameObject("Cmd_" + type);
        go.transform.SetParent(transform, false);
        var universal = go.AddComponent<UniversalCodeBlock>();
        universal.blockType = type;
        return universal;
    }

    CodeBlock GetPrefab(BlockType type)
    {
        switch (type)
        {
            case BlockType.Start:     return startBlockPrefab;
            case BlockType.Walk:      return walkBlockPrefab;
            case BlockType.Lava:      return lavaBlockPrefab;
            case BlockType.Stop:      return stopBlockPrefab;
            case BlockType.MoveLeft:  return leftBlockPrefab;
            case BlockType.MoveRight: return rightBlockPrefab;
            case BlockType.Force:     return forceBlockPrefab;
            case BlockType.Wait:      return waitBlockPrefab;
            default:                  return null;
        }
    }

    public void StopProgram()
    {
        if (codeExecutor != null) codeExecutor.StopExecution();
        ClearExecutionHighlights();
        UpdatePlayButtonVisual(false);

        var robot = player != null ? player : codeExecutor != null ? codeExecutor.player : null;
        if (robot == null) return;
        robot.GetComponent<PlayerRespawn>()?.CancelPendingRespawn();
        if (robot.currentState == PlayerState.Dead) robot.PlaceOnStartTile();
        else robot.StopAtCurrentTile();
        SetStatusBanner("LOOP PAUSED — CLICK PLAY TO RUN OR RESET TO RETURN TO START");
    }

    public void ResetProgram()
    {
        ClearExecutionHighlights();
        UpdatePlayButtonVisual(false);
        HideVictoryBanner();

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
        foreach (var old in spawnedCommands)
        {
            if (old != null) Destroy(old.gameObject);
        }
        spawnedCommands.Clear();
        SetStatusBanner("ROBOT RESET TO START TILE — READY TO DEPLOY");
    }

    // =====================================================================
    // Live Execution Feedback & Victory UI
    // =====================================================================

    private void HandleStepStarted(int stepIndex, BlockType type)
    {
        highlightedExecutionNodeIndex = stepIndex;
        var wired = GetWiredGraphNodes();
        for (int i = 0; i < wired.Count; i++)
        {
            if (wired[i] != null)
                wired[i].SetExecutionHighlight(i == stepIndex);
        }
        RefreshWires();
    }

    private void HandleExecutionEnded()
    {
        ClearExecutionHighlights();
        UpdatePlayButtonVisual(false);
    }

    private void HandleStatusMessage(string msg)
    {
        SetStatusBanner(msg);
    }

    private void ClearExecutionHighlights()
    {
        highlightedExecutionNodeIndex = -1;
        foreach (var n in graphNodes)
        {
            if (n != null) n.SetExecutionHighlight(false);
        }
        RefreshWires();
    }

    private void UpdatePlayButtonVisual(bool running)
    {
        if (centerPlayBtnIcon != null)
        {
            Sprite s = running ? stopBtnSprite : playBtnSprite;
            if (s != null) centerPlayBtnIcon.sprite = s;
        }
        if (centerPlayStateLabel != null)
        {
            centerPlayStateLabel.text = running ? "STOP" : "RUN LOOP";
        }
    }

    public void SetStatusBanner(string text)
    {
        if (statusBannerLabel != null)
            statusBannerLabel.text = text;
    }

    public void ShowVictoryBanner(LevelDefinition level, bool hasNextLevel)
    {
        if (victoryOverlay == null) return;
        victoryOverlay.SetActive(true);
        victoryOverlay.transform.SetAsLastSibling();

        if (victoryTitleText != null)
            victoryTitleText.text = $"{level.title} CLEARED!";
        if (victorySubText != null)
        {
            victorySubText.text = hasNextLevel
                ? $"LOOP VERIFIED IN {commands.Count} NODE(S) — NEXT NODE MODIFIER UNLOCKED!"
                : "ALL 7 CHRONO-LOOP PROTOCOLS COMPLETED! CONGRATULATIONS!";
        }
    }

    public void HideVictoryBanner()
    {
        if (victoryOverlay != null)
            victoryOverlay.SetActive(false);
    }

    // =====================================================================
    // UI Construction Helpers
    // =====================================================================

    private static GameObject CreateUiObject(string name, Transform parent, bool withImage = false, bool withButton = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        if (withImage) go.AddComponent<Image>();
        if (withButton) go.AddComponent<Button>();
        return go;
    }

    private static TextMeshProUGUI CreateStyledText(
        Transform parent,
        string name,
        string content,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        StretchFull(tmp.rectTransform);
        return tmp;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
