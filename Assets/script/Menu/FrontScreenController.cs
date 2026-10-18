using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runtime title screen for the existing campaign scenes. No scene or board rewrite is needed.
[DefaultExecutionOrder(1000)]
public sealed class FrontScreenController : MonoBehaviour
{
    public static FrontScreenController Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.menuRoot != null && Instance.menuRoot.activeSelf;

    static readonly Color Lavender = new Color(.78f, .61f, .81f);
    static readonly Color Plum = new Color(.16f, .085f, .18f);
    static readonly Color Cream = new Color(.98f, .94f, .89f);
    static readonly Color Lime = new Color(.35f, 1f, .035f);
    GameObject menuRoot, soundPanel;
    RectTransform safeRoot;
    Button playButton, soundButton, exitButton, menuButton;
    TextMeshProUGUI playLabel, musicVolumeLabel, sfxVolumeLabel;
    Slider musicVolumeSlider, sfxVolumeSlider;
    Sprite rounded;
    float previousTimeScale = 1f;
    bool hasPlayed;
    Rect lastSafeArea;
    Vector2Int lastScreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != null || Object.FindFirstObjectByType<LevelManager>() == null) return;
        new GameObject("Front Screen").AddComponent<FrontScreenController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        rounded = CreateRoundedSprite();
        if (EventSystem.current == null)
            new GameObject("Menu EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();

        menuRoot = new GameObject("Title Screen", typeof(RectTransform));
        menuRoot.transform.SetParent(transform, false);
        Stretch(menuRoot.GetComponent<RectTransform>());
        var fill = menuRoot.AddComponent<Image>();
        fill.color = Plum;
        safeRoot = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
        safeRoot.SetParent(menuRoot.transform, false);
        Stretch(safeRoot);
        var content = new GameObject("Landscape Composition", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(safeRoot, false);
        Stretch(content);
        var fit = content.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 16f / 9f;
        // A fixed design surface follows the fitted composition at every screen size.
        var design = new GameObject("Design 1920x1080", typeof(RectTransform)).GetComponent<RectTransform>();
        design.SetParent(content, false);
        design.anchorMin = design.anchorMax = design.pivot = new Vector2(.5f, .5f);
        design.sizeDelta = new Vector2(1920, 1080);
        var sizing = content.gameObject.AddComponent<MenuDesignScale>();
        sizing.design = design;
        var bg = new GameObject("Factory Title Artwork", typeof(RectTransform), typeof(RawImage));
        bg.transform.SetParent(design, false);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<RawImage>().texture = Resources.Load<Texture2D>("FrontScreen/FactoryTitle");
        bg.GetComponent<RawImage>().raycastTarget = false;

        playButton = Card(design, "Play", "PLAY", MenuSymbol.Play, 310, BeginOrResume);
        playLabel = playButton.GetComponentInChildren<TextMeshProUGUI>();
        soundButton = Card(design, "Sound", "SOUND", MenuSymbol.Sound, 560, OpenSound);
        exitButton = Card(design, "Exit", "EXIT", MenuSymbol.Exit, 810, ExitGame);
        Navigation vertical = new Navigation { mode = Navigation.Mode.Explicit };
        vertical.selectOnUp = exitButton; vertical.selectOnDown = soundButton; playButton.navigation = vertical;
        vertical.selectOnUp = playButton; vertical.selectOnDown = exitButton; soundButton.navigation = vertical;
        vertical.selectOnUp = soundButton; vertical.selectOnDown = playButton; exitButton.navigation = vertical;
        BuildSoundPanel(design);

        menuButton = SmallButton(transform, "Open Menu", "MENU", new Rect(18, 18, 110, 42), OpenMenu);
        menuButton.gameObject.SetActive(false);
        UpdateSafeArea();
        OpenMenu();
    }

    void Update()
    {
        if (lastSafeArea != Screen.safeArea || lastScreen != new Vector2Int(Screen.width, Screen.height)) UpdateSafeArea();
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (soundPanel != null && soundPanel.activeSelf) CloseSound();
        else if (IsOpen && hasPlayed) BeginOrResume();
        else if (!IsOpen) OpenMenu();
    }

    void UpdateSafeArea()
    {
        lastSafeArea = Screen.safeArea;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
        if (safeRoot == null || Screen.width == 0 || Screen.height == 0) return;
        safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
    }

    public void OpenMenu()
    {
        if (!menuRoot.activeSelf || !hasPlayed) previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        menuRoot.SetActive(true);
        soundPanel.SetActive(false);
        menuButton.gameObject.SetActive(false);
        playLabel.text = hasPlayed ? "RESUME" : "PLAY";
        EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
    }

    public void BeginOrResume()
    {
        GameVolume.Save();
        hasPlayed = true;
        soundPanel.SetActive(false);
        menuRoot.SetActive(false);
        menuButton.gameObject.SetActive(true);
        Time.timeScale = previousTimeScale;
        if (GameManager.Instance != null) GameManager.Instance.currentState = GameState.Playing;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void OpenSound()
    {
        soundPanel.SetActive(true);
        playButton.interactable = soundButton.interactable = exitButton.interactable = false;
        musicVolumeSlider.SetValueWithoutNotify(GameVolume.Music);
        sfxVolumeSlider.SetValueWithoutNotify(GameVolume.Sfx);
        UpdateVolumeLabels();
        EventSystem.current?.SetSelectedGameObject(musicVolumeSlider.gameObject);
    }

    public void CloseSound()
    {
        GameVolume.Save();
        playButton.interactable = soundButton.interactable = exitButton.interactable = true;
        soundPanel.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(soundButton.gameObject);
    }

    public void SetMusicVolume(float value)
    {
        GameVolume.SetMusic(value);
        UpdateVolumeLabels();
    }

    public void SetSfxVolume(float value)
    {
        GameVolume.SetSfx(value);
        UpdateVolumeLabels();
    }

    void UpdateVolumeLabels()
    {
        musicVolumeLabel.text = Mathf.RoundToInt(GameVolume.Music * 100f) + "%";
        sfxVolumeLabel.text = Mathf.RoundToInt(GameVolume.Sfx * 100f) + "%";
    }

    void OnApplicationPause(bool paused) { if (paused) GameVolume.Save(); }
    void OnApplicationQuit() { GameVolume.Save(); }
    void OnDestroy()
    {
        if (Instance != this) return;
        if (IsOpen) Time.timeScale = previousTimeScale;
        Instance = null;
        if (rounded != null) { Destroy(rounded.texture); Destroy(rounded); }
    }

    void ExitGame()
    {
        GameVolume.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_IOS
        // iOS applications are closed by the system; return to the title's resting state.
        EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
#else
        Application.Quit();
#endif
    }

    Button Card(Transform parent, string name, string label, MenuSymbol symbol, float y, UnityEngine.Events.UnityAction action)
    {
        var card = Panel(parent, name + " Button", new Rect(1570, y, 252, 228), Lavender);
        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.1f, 1.15f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.78f, .7f, .8f);
        colors.fadeDuration = .12f;
        button.colors = colors;
        button.onClick.AddListener(action);
        var header = Panel(card, "Header", new Rect(10, 10, 232, 54), Plum);
        var pill = Panel(header, "Connector Slot", new Rect(17, 17, 66, 24), Lavender);
        Panel(pill, "Slot Inset", new Rect(6, 6, 54, 12), Plum);
        Text(header, "Label", label, new Rect(94, 0, 126, 54), 30, Lavender, TextAlignmentOptions.Center);
        var inset = Panel(card, "Symbol Inset", new Rect(28, 84, 157, 130), Plum);
        var icon = new GameObject("Symbol", typeof(RectTransform), typeof(MenuSymbolGraphic));
        icon.transform.SetParent(inset, false);
        SetRect(icon.GetComponent<RectTransform>(), new Rect(31, 20, 96, 90));
        var graphic = icon.GetComponent<MenuSymbolGraphic>();
        graphic.symbol = symbol;
        graphic.color = Lavender;
        graphic.raycastTarget = false;
        var port = Panel(card, "Connection Port Border", new Rect(238, 121, 30, 30), Lavender);
        Panel(port, "Lime Connection Port", new Rect(5, 5, 20, 20), Lime);
        // Only the real button receives raycasts, including keyboard/controller selection.
        foreach (var image in card.GetComponentsInChildren<Image>()) image.raycastTarget = image.transform == card;
        return button;
    }

    void BuildSoundPanel(Transform design)
    {
        var overlay = Panel(design, "Sound Settings", new Rect(0, 0, 1920, 1080), new Color(.045f, .025f, .06f, .84f));
        overlay.GetComponent<Image>().sprite = null;
        overlay.GetComponent<Image>().type = Image.Type.Simple;
        soundPanel = overlay.gameObject;
        var frame = Panel(overlay, "Volume Panel", new Rect(550, 250, 820, 580), Lavender);
        var panel = Panel(frame, "Plum Inset", new Rect(6, 6, 808, 568), Plum);
        Text(panel, "Title", "SOUND", new Rect(48, 28, 500, 56), 42, Cream);
        musicVolumeSlider = BuildVolumeRow(panel, "Music", "MUSIC", 112, SetMusicVolume, out musicVolumeLabel);
        sfxVolumeSlider = BuildVolumeRow(panel, "SFX", "SOUND EFFECTS", 280, SetSfxVolume, out sfxVolumeLabel);
        SmallButton(panel, "Mute", "MUTE ALL", new Rect(48, 470, 180, 58), () =>
        {
            musicVolumeSlider.value = 0f;
            sfxVolumeSlider.value = 0f;
        });
        SmallButton(panel, "Done", "DONE", new Rect(590, 470, 170, 58), CloseSound);
        Text(panel, "Save Hint", "VOLUMES SAVED AUTOMATICALLY", new Rect(235, 478, 345, 40), 16, Lavender, TextAlignmentOptions.Center);
        soundPanel.SetActive(false);
    }

    Slider BuildVolumeRow(Transform panel, string name, string caption, float y,
        UnityEngine.Events.UnityAction<float> onChanged, out TextMeshProUGUI percentLabel)
    {
        Text(panel, name + " Description", caption, new Rect(48, y, 520, 36), 24, Lavender);
        percentLabel = Text(panel, name + " Volume Percent", "100%", new Rect(620, y - 2, 140, 42), 32, Cream, TextAlignmentOptions.Right);
        var sliderRoot = new GameObject(name + " Volume Slider", typeof(RectTransform), typeof(Slider));
        sliderRoot.transform.SetParent(panel, false);
        SetRect(sliderRoot.GetComponent<RectTransform>(), new Rect(132, y + 64, 544, 76));
        var slider = sliderRoot.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;
        var track = Panel(sliderRoot.transform, "Track", new Rect(0, 26, 544, 24), new Color(.36f, .24f, .39f));
        var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(track, false); Stretch(fillArea);
        var fill = Panel(fillArea, "Volume Fill", new Rect(0, 0, 544, 24), Lavender);
        Stretch(fill);
        slider.fillRect = fill;
        var handleArea = new GameObject("Handle Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(sliderRoot.transform, false); Stretch(handleArea);
        handleArea.offsetMin = new Vector2(18, 0); handleArea.offsetMax = new Vector2(-18, 0);
        var handle = Panel(handleArea, "Handle", new Rect(0, 0, 36, 64), Cream);
        handle.anchorMin = new Vector2(0, .5f); handle.anchorMax = new Vector2(0, .5f);
        handle.pivot = new Vector2(.5f, .5f); handle.anchoredPosition = Vector2.zero;
        handle.sizeDelta = new Vector2(36, -12);
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        var navigation = slider.navigation; navigation.mode = Navigation.Mode.Automatic; slider.navigation = navigation;
        slider.onValueChanged.AddListener(onChanged);
        SmallButton(panel, "Decrease " + name + " Volume", "−", new Rect(48, y + 70, 64, 64), () => slider.value -= .05f);
        SmallButton(panel, "Increase " + name + " Volume", "+", new Rect(696, y + 70, 64, 64), () => slider.value += .05f);
        return slider;
    }

    Button SmallButton(Transform parent, string name, string label, Rect rect, UnityEngine.Events.UnityAction action)
    {
        var panel = Panel(parent, name, rect, Lavender);
        var b = panel.gameObject.AddComponent<Button>(); b.targetGraphic = panel.GetComponent<Image>();
        b.onClick.AddListener(action);
        Text(panel, "Label", label, new Rect(0, 0, rect.width, rect.height), 23, Plum, TextAlignmentOptions.Center);
        return b;
    }

    RectTransform Panel(Transform parent, string name, Rect rect, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); SetRect(rt, rect);
        var image = go.GetComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color;
        return rt;
    }

    static TextMeshProUGUI Text(Transform parent, string name, string value, Rect rect, float size, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false); SetRect(go.GetComponent<RectTransform>(), rect);
        var text = go.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size;
        text.fontStyle = FontStyles.Bold; text.color = color; text.alignment = alignment;
        text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    static void SetRect(RectTransform rt, Rect rect)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(rect.x, -rect.y); rt.sizeDelta = rect.size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Sprite CreateRoundedSprite()
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { name = "Menu Rounded Panel", filterMode = FilterMode.Bilinear };
        var pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            float dx = Mathf.Max(14f - x - .5f, x + .5f - 50f, 0);
            float dy = Mathf.Max(14f - y - .5f, y + .5f - 50f, 0);
            pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(14f - Mathf.Sqrt(dx * dx + dy * dy)));
        }
        texture.SetPixels(pixels); texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
    }
}

// Scale the fixed design to its safe-area aspect-fitted rectangle, independent of CanvasScaler.
public sealed class MenuDesignScale : MonoBehaviour
{
    public RectTransform design;
    void LateUpdate()
    {
        if (design != null) design.localScale = Vector3.one * (((RectTransform)transform).rect.width / 1920f);
    }
}

public enum MenuSymbol { Play, Sound, Exit }

public sealed class MenuSymbolGraphic : MaskableGraphic
{
    public MenuSymbol symbol;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (symbol == MenuSymbol.Play) Polygon(vh, new Vector2(22, 8), new Vector2(22, 82), new Vector2(82, 45));
        else if (symbol == MenuSymbol.Sound)
        {
            Polygon(vh, new Vector2(8, 29), new Vector2(8, 61), new Vector2(27, 61), new Vector2(49, 80), new Vector2(49, 10), new Vector2(27, 29));
            Arc(vh, new Vector2(48, 45), 24, -55, 55, 6);
            Arc(vh, new Vector2(48, 45), 41, -55, 55, 6);
        }
        else
        {
            Line(vh, new Vector2(49, 12), new Vector2(80, 12), 7);
            Line(vh, new Vector2(80, 12), new Vector2(80, 80), 7);
            Line(vh, new Vector2(80, 80), new Vector2(49, 80), 7);
            Polygon(vh, new Vector2(53, 19), new Vector2(73, 13), new Vector2(73, 80), new Vector2(53, 73));
            Line(vh, new Vector2(51, 45), new Vector2(10, 45), 8);
            Line(vh, new Vector2(10, 45), new Vector2(27, 62), 8);
            Line(vh, new Vector2(10, 45), new Vector2(27, 28), 8);
        }
    }
    void Polygon(VertexHelper vh, params Vector2[] points)
    {
        int start = vh.currentVertCount;
        foreach (var p in points) vh.AddVert(new Vector3(p.x, p.y - 90f), color, Vector2.zero);
        for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
    }
    void Line(VertexHelper vh, Vector2 from, Vector2 to, float width)
    {
        Vector2 n = new Vector2(-(to - from).y, (to - from).x).normalized * width * .5f;
        Polygon(vh, from - n, from + n, to + n, to - n);
    }
    void Arc(VertexHelper vh, Vector2 center, float radius, float from, float to, float width)
    {
        for (int i = 0; i < 16; i++)
        {
            float a = Mathf.Lerp(from, to, i / 16f) * Mathf.Deg2Rad;
            float b = Mathf.Lerp(from, to, (i + 1) / 16f) * Mathf.Deg2Rad;
            Line(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width);
        }
    }
}
