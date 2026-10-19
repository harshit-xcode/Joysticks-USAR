using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// An isolated, render-texture-backed 3D stage. Display meshes are shared with the
// campaign, but no cloned Tile/Player behaviours can register with gameplay.
[DefaultExecutionOrder(1000)]
public sealed class FrontScreenController : MonoBehaviour
{
    public static FrontScreenController Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.menuRoot != null && Instance.menuRoot.activeSelf;
    const int StageLayer = 30;
    static readonly Color Ink = new Color(.025f, .055f, .09f);
    static readonly Color Ice = new Color(.27f, .9f, .95f);
    static readonly Color Gold = new Color(1f, .7f, .27f);
    GameObject menuRoot, stage, levelPanel, settingsPanel;
    RectTransform safeRoot, viewportRoot;
    Camera stageCamera;
    RenderTexture stageTexture;
    RawImage sceneImage;
    Transform hero;
    ClaudeMascotAnimator mascot;
    readonly List<Transform> nodeRoots = new List<Transform>();
    readonly List<RectTransform> menuCards = new List<RectTransform>();
    readonly List<RectTransform> menuPorts = new List<RectTransform>();
    readonly List<Image> nodeLeds = new List<Image>();
    readonly List<Color> nodeColors = new List<Color>();
    readonly List<Material> ownedMaterials = new List<Material>();
    readonly Dictionary<Light, int> sceneLightMasks = new Dictionary<Light, int>();
    Material duskSky, atmosphere;
    float skyRotation, skyTime;
    LineRenderer wire;
    TextMeshProUGUI instruction, resumeLabel;
    Button menuButton, resumeButton;
    bool hasPlayed, wiring, busy, dragging, keyboardMode;
    float savedTimeScale = 1f, phase;
    int keyboardTarget = 1;
    Vector2 pointerStart;
    Rect safeArea;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance == null && FindFirstObjectByType<LevelManager>() != null)
            new GameObject("Adventure Title Stage").AddComponent<FrontScreenController>();
    }
    void Awake() { Instance = this; }
    void Start()
    {
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
        menuRoot = new GameObject("3D Title", typeof(RectTransform), typeof(Image));
        menuRoot.transform.SetParent(transform, false); Stretch((RectTransform)menuRoot.transform);
        menuRoot.GetComponent<Image>().color = Ink;
        viewportRoot = new GameObject("Safe viewport", typeof(RectTransform)).GetComponent<RectTransform>();
        viewportRoot.SetParent(menuRoot.transform, false); Stretch(viewportRoot);
        safeRoot = new GameObject("1920 x 1080 composition", typeof(RectTransform)).GetComponent<RectTransform>();
        safeRoot.SetParent(viewportRoot, false);
        safeRoot.anchorMin = safeRoot.anchorMax = safeRoot.pivot = new Vector2(.5f,.5f);
        safeRoot.sizeDelta = new Vector2(1920,1080);
        sceneImage = new GameObject("Live 3D stage", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        sceneImage.transform.SetParent(safeRoot, false); Stretch(sceneImage.rectTransform);
        sceneImage.raycastTarget = false;
        BuildStage();
        BuildTitle();
        instruction = Label(safeRoot, "Drag from Start to an option.", new Vector2(.06f, .12f), new Vector2(1500, 54), 28, Color.white);
        BuildLevels(); BuildSettings();
        resumeButton = ButtonAt(safeRoot, "Resume", new Vector2(.735f, .95f), new Vector2(180, 42), BeginOrResume);
        resumeLabel = resumeButton.GetComponentInChildren<TextMeshProUGUI>();
        ButtonAt(safeRoot, "Audio", new Vector2(.84f, .95f), new Vector2(170, 42), () => SetPanel(settingsPanel, true));
        menuButton = ButtonAt(transform, "Menu · Esc", new Vector2(.012f, .98f), new Vector2(160, 42), OpenMenu);
        OpenMenu();
    }
    void BuildStage()
    {
        stage = new GameObject("Title Diorama — display geometry only");
        stage.transform.position = new Vector3(5000, 0, 5000);
        stageCamera = new GameObject("Title Camera").AddComponent<Camera>();
        stageCamera.transform.SetParent(stage.transform, false);
        stageCamera.transform.localPosition = new Vector3(12, 13, -18);
        stageCamera.transform.LookAt(stage.transform.position);
        stageCamera.orthographic = true; stageCamera.orthographicSize = 6.5f;
        stageCamera.clearFlags = CameraClearFlags.Skybox; stageCamera.backgroundColor = Ink;
        stageCamera.aspect = 16f / 9f;
        // Keep the campaign's directional lights from overexposing the title bot.
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            sceneLightMasks[light] = light.cullingMask;
            light.cullingMask &= ~(1 << StageLayer);
        }
        BuildDuskSky();
        stageCamera.cullingMask = 1 << StageLayer; stageCamera.nearClipPlane = .1f; stageCamera.farClipPlane = 100;
        var key = new GameObject("Warm studio key").AddComponent<Light>();
        key.transform.SetParent(stage.transform, false); key.type = LightType.Directional;
        key.transform.rotation = Quaternion.Euler(38, -32, 0); key.intensity = .95f;
        key.color = new Color(1, .96f, .9f); key.cullingMask = 1 << StageLayer;
        var fill = new GameObject("Cool rim").AddComponent<Light>();
        fill.transform.SetParent(stage.transform, false); fill.type = LightType.Directional;
        fill.transform.rotation = Quaternion.Euler(25, 145, 0); fill.intensity = .45f; fill.color = new Color(.65f, .84f, 1f); fill.cullingMask = 1 << StageLayer;
        var manager = LevelManager.Instance;
        var display = new GameObject("Campaign platform assembly").transform;
        display.SetParent(stage.transform, false);
        display.position = stage.transform.position + stageCamera.transform.right * -4.7f + stageCamera.transform.up * -.8f;
        // Copy the actual current campaign deck, start pad and gate meshes/materials.
        var tiles = manager.boardRoot.GetComponentsInChildren<Tile>();
        Tile start = null, free = null, end = null;
        foreach (var t in tiles)
        {
            if (t is StartTile) start = t;
            else if (t is EndTile) end = t;
            else if (t is FreeTile && free == null) free = t;
        }
        Vector3[] positions = { new Vector3(-2,0,0), Vector3.zero, new Vector3(2,0,0), new Vector3(2,0,2), new Vector3(0,0,2), new Vector3(-2,0,2) };
        for (int i = 0; i < positions.Length; i++)
        {
            var cell = new GameObject("Shared level tile " + i).transform; cell.SetParent(display, false); cell.localPosition = positions[i];
            CopyMeshes(i == 1 ? start : i == 3 ? end : free, cell);
        }
        hero = new GameObject("Joyful main character").transform; hero.SetParent(display, false);
        mascot = hero.gameObject.AddComponent<ClaudeMascotAnimator>();
        mascot.ConstructGeometry(hero); mascot.enabled = false;
        mascot.SetExpression(ClaudeMascotAnimator.EyeExpression.HappyArcs);
        hero.localScale = Vector3.one * 1.55f;
        mascot.mascotPivot.rotation = Quaternion.Euler(0, stageCamera.transform.eulerAngles.y + 174, 0);
        // The same eight-legged Claude rig as gameplay, with a broad, readable face.
        mascot.mascotPivot.localScale = new Vector3(1.45f, 1.25f, 1.32f);
        for (int i = 0; i < 22; i++)
        {
            var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spark.name = "Celebration spark"; spark.transform.SetParent(display, false);
            float a = i * 2.39996f;
            spark.transform.localPosition = new Vector3(Mathf.Cos(a) * (2.2f + i * .04f), 1.2f + (i % 5) * .47f, Mathf.Sin(a) * 2);
            spark.transform.localRotation = Quaternion.Euler(i * 17, i * 29, 35);
            spark.transform.localScale = new Vector3(.075f, .16f, .075f);
            spark.GetComponent<Renderer>().sharedMaterial = Material(i % 2 == 0 ? Ice : Gold, true);
            Destroy(spark.GetComponent<Collider>());
        }
        BuildNode("Start", new Vector2(.57f, .51f), CommandLabels.LedColorFor(BlockType.Start));
        BuildNode("New game", new Vector2(.83f, .70f), Gold);
        BuildNode("Levels", new Vector2(.83f, .49f), Ice);
        BuildNode("Exit", new Vector2(.83f, .28f), new Color(1, .46f, .38f));
        wire = new GameObject("Live connection cable").AddComponent<LineRenderer>();
        wire.transform.SetParent(stage.transform, false); wire.gameObject.layer = StageLayer;
        wire.sharedMaterial = Material(Ice, true); wire.positionCount = 48;
        wire.startWidth = wire.endWidth = .045f; wire.numCapVertices = 8; wire.enabled = false;
        SetLayer(stage.transform);
    }
    void CopyMeshes(Tile source, Transform target)
    {
        if (source == null) return;
        foreach (var mesh in source.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = mesh.GetComponent<MeshRenderer>(); if (renderer == null) continue;
            var go = new GameObject(mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(target, false);
            go.transform.localPosition = source.transform.InverseTransformPoint(mesh.transform.position);
            go.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * mesh.transform.rotation;
            go.transform.localScale = mesh.transform.lossyScale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;
                // Title-only tints match the approved blue-gray deck without changing level assets.
                string materialName = materials[i].name;
                Color? tint = materialName.Contains("TileTop") ? new Color(.38f, .49f, .54f) :
                    materialName.Contains("DarkChassis") ? new Color(.21f, .24f, .28f) :
                    materialName.Contains("StrutDark") ? new Color(.19f, .2f, .23f) : (Color?)null;
                if (!tint.HasValue) continue;
                var copy = new Material(materials[i]) { name = materialName + " title lighting" };
                copy.color = tint.Value;
                if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", tint.Value);
                ownedMaterials.Add(copy); materials[i] = copy;
            }
            go.GetComponent<MeshRenderer>().sharedMaterials = materials;
        }
    }
    void BuildNode(string caption, Vector2 viewport, Color color)
    {
        // The title uses the same frame, header LED and ports as gameplay nodes.
        // These cards have no NodeCardUI/NodePortUI behaviours, so menu wiring stays isolated.
        int index = nodeRoots.Count;
        var anchor = new GameObject(caption + " wire anchor").transform;
        anchor.SetParent(stage.transform, false);
        anchor.position = stageCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, 15));
        nodeRoots.Add(anchor);
        var go = new GameObject(caption + " menu node", typeof(RectTransform), typeof(Image));
        var card = go.GetComponent<RectTransform>(); card.SetParent(safeRoot, false);
        card.anchorMin = card.anchorMax = viewport; card.pivot = new Vector2(.5f, .5f);
        card.sizeDelta = new Vector2(168, 168);
        var background = go.GetComponent<Image>();
        background.sprite = UIManager.LoadUiSprite("NodeCardFrame"); background.color = Color.white;
        background.raycastTarget = false;
        menuCards.Add(card); nodeColors.Add(color);

        var led = NodeImage(card, "Header LED", new Vector2(0,1), new Vector2(32,-25), new Vector2(30,11), "LedPill", color);
        nodeLeds.Add(led);
        var title = Label(card, caption, new Vector2(1,1), new Vector2(124,28), 18.5f, color);
        title.rectTransform.pivot = new Vector2(1,1); title.rectTransform.anchoredPosition = new Vector2(-12,-13);
        title.alignment = TextAlignmentOptions.Right;

        if (index == 0)
            NodeImage(card, "START action", new Vector2(.5f,.47f), Vector2.zero, new Vector2(76,76), "Icon_Start", color);
        else
        {
            var icon = new GameObject(caption + " action", typeof(RectTransform), typeof(CanvasRenderer), typeof(TitleNodeGlyph));
            var rect = icon.GetComponent<RectTransform>(); rect.SetParent(card, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,.47f); rect.pivot = new Vector2(.5f,.5f);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(76,76);
            var graphic = icon.GetComponent<TitleNodeGlyph>(); graphic.symbol = index; graphic.color = color; graphic.raycastTarget = false;
        }
        var port = NodeImage(card, index == 0 ? "Output port" : "Input port",
            new Vector2(index == 0 ? 1 : 0,.5f), new Vector2(index == 0 ? 6 : -6,-3), new Vector2(36,36),
            index == 0 ? "PortTriangle" : "PortSquare", color);
        menuPorts.Add(port.rectTransform);
    }
    static Image NodeImage(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, string sprite, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = anchor;
        image.rectTransform.pivot = new Vector2(.5f,.5f); image.rectTransform.anchoredPosition = position; image.rectTransform.sizeDelta = size;
        image.sprite = UIManager.LoadUiSprite(sprite); image.color = color; image.preserveAspect = true; image.raycastTarget = false;
        return image;
    }
    Vector3 PortWorld(int index)
    {
        // UI ports sit 90 design pixels to the side and 3 pixels below their card center.
        float unitsPerPixel = stageCamera.orthographicSize * 2 / 1080f;
        return nodeRoots[index].position + stageCamera.transform.right * ((index == 0 ? 90 : -90) * unitsPerPixel)
            - stageCamera.transform.up * (3 * unitsPerPixel);
    }
    void BuildDuskSky()
    {
        var source = Resources.Load<Material>("FrontScreen/SkyEarlyDusk");
        if (source != null)
        {
            // Clone so neither the level sky nor the authored material is modified.
            duskSky = new Material(source) { name = "JOYSTIX BOT animated Early Dusk" };
            ownedMaterials.Add(duskSky);
            skyRotation = duskSky.GetFloat("_Rotation");
            stageCamera.gameObject.AddComponent<Skybox>().material = duskSky;
        }
        else
        {
            stageCamera.clearFlags = CameraClearFlags.SolidColor;
            Debug.LogWarning("JOYSTIX BOT: FrontScreen/SkyEarlyDusk resource is missing.");
        }
        var shader = Resources.Load<Shader>("FrontScreen/TitleAtmosphere");
        if (shader == null || !shader.isSupported) return;
#if UNITY_EDITOR
        // An atmosphere import failure must leave the actual skybox visible, never magenta.
        if (UnityEditor.ShaderUtil.ShaderHasError(shader)) return;
#endif
        atmosphere = new Material(shader) { name = "Gentle cloud drift and star twinkle" };
        ownedMaterials.Add(atmosphere);
        var sky = GameObject.CreatePrimitive(PrimitiveType.Quad);
        sky.name = "Animated dusk atmosphere — behind title geometry";
        sky.transform.SetParent(stageCamera.transform, false);
        sky.transform.localPosition = new Vector3(0, 0, 80);
        sky.transform.localScale = new Vector3(13 * (16f / 9f), 13, 1);
        sky.GetComponent<Renderer>().sharedMaterial = atmosphere;
        Destroy(sky.GetComponent<Collider>());
    }
    void AnimateDuskSky()
    {
        // Deliberately independent of the paused gameplay clock.
        skyTime += Time.unscaledDeltaTime;
        if (duskSky != null) duskSky.SetFloat("_Rotation", Mathf.Repeat(skyRotation + skyTime * .12f, 360));
        if (atmosphere != null) atmosphere.SetFloat("_MenuTime", skyTime);
    }
    Material Material(Color color, bool emissive = false)
    {
        var mat = new Material(Shader.Find("Standard")); mat.color = color;
        if (emissive) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * .7f); }
        ownedMaterials.Add(mat); return mat;
    }
    void Update()
    {
        if (safeRoot == null) return;
        UpdateSurface();
        if (!IsOpen) { if (Input.GetKeyDown(KeyCode.Escape)) OpenMenu(); return; }
        AnimateHero();
        AnimateDuskSky();
        if (busy) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (levelPanel.activeSelf) SetPanel(levelPanel, false);
            else if (settingsPanel.activeSelf) SetPanel(settingsPanel, false);
            else if (wiring) CancelWire();
            else if (hasPlayed) BeginOrResume();
        }
        if (levelPanel.activeSelf || settingsPanel.activeSelf || busy) return;
        if (Input.GetKeyDown(KeyCode.UpArrow)) { keyboardTarget = (keyboardTarget + 1) % 3 + 1; keyboardMode = true; }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { keyboardTarget = keyboardTarget % 3 + 1; keyboardMode = true; }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            keyboardMode = true; if (!wiring) ArmWire(); else StartCoroutine(Connect(keyboardTarget));
        }
        int hit = HitNode(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            pointerStart = Input.mousePosition; dragging = false; keyboardMode = false;
            if (hit == 0) { if (wiring) CancelWire(); else ArmWire(); }
            else if (wiring && hit > 0) StartCoroutine(Connect(hit));
        }
        if (Input.GetMouseButton(0) && Vector2.Distance(pointerStart, Input.mousePosition) > 9) dragging = true;
        if (Input.GetMouseButtonUp(0) && dragging && wiring)
        {
            if (hit > 0) StartCoroutine(Connect(hit)); else CancelWire();
        }
        if (wiring && !busy)
        {
            Vector3 end;
            // Viewport mapping stays correct when safe area and texture dimensions differ.
            end = stageCamera.ViewportToWorldPoint(new Vector3((Input.mousePosition.x-safeArea.x)/safeArea.width, (Input.mousePosition.y-safeArea.y)/safeArea.height, 15));
            if (hit > 0) end = PortWorld(hit);
            else if (keyboardMode) end = PortWorld(keyboardTarget);
            DrawWire(end);
        }
        for (int i = 0; i < nodeLeds.Count; i++)
            nodeLeds[i].color = hit == i || wiring && i == keyboardTarget ? Color.Lerp(nodeColors[i], Color.white, .18f) : nodeColors[i];
    }
    void UpdateSurface()
    {
        safeArea = Screen.safeArea;
        if (Screen.width < 1 || Screen.height < 1) return;
        viewportRoot.anchorMin = new Vector2(safeArea.xMin/Screen.width, safeArea.yMin/Screen.height);
        viewportRoot.anchorMax = new Vector2(safeArea.xMax/Screen.width, safeArea.yMax/Screen.height);
        // Keep the complete composition visible, including on ultrawide and portrait displays.
        float w = safeArea.width, h = safeArea.height;
        if (w/h > 16f/9) w = h*16f/9; else h = w*9f/16;
        viewportRoot.offsetMin = new Vector2((safeArea.width-w)/2, (safeArea.height-h)/2);
        viewportRoot.offsetMax = -viewportRoot.offsetMin;
        safeRoot.localScale = Vector3.one * (viewportRoot.rect.width / 1920f);
        safeArea = new Rect(safeArea.x+(safeArea.width-w)/2, safeArea.y+(safeArea.height-h)/2,w,h);
        int tw = Mathf.Max(1, Mathf.RoundToInt(w)), th = Mathf.Max(1, Mathf.RoundToInt(h));
        if (stageTexture != null && stageTexture.width == tw && stageTexture.height == th) return;
        if (stageTexture != null) { stageCamera.targetTexture = null; stageTexture.Release(); Destroy(stageTexture); }
        stageTexture = new RenderTexture(tw, th, 24) { name = "Title stage live view", antiAliasing = 2 };
        stageCamera.targetTexture = stageTexture; stageCamera.aspect = 16f/9; sceneImage.texture = stageTexture;
        for (int i = 0; i < nodeRoots.Count; i++)
        {
            Vector2 p = i == 0 ? new Vector2(.57f,.51f) : new Vector2(.83f, i == 1 ? .70f : i == 2 ? .49f : .28f);
            nodeRoots[i].position = stageCamera.ViewportToWorldPoint(new Vector3(p.x,p.y,15));
        }
    }
    int HitNode(Vector2 screen)
    {
        if (!safeArea.Contains(screen)) return -1;
        // Rectangular card/port hit areas, independent of paused physics.
        for (int i = 0; i < menuCards.Count; i++)
            if (RectTransformUtility.RectangleContainsScreenPoint(menuCards[i], screen, null)
                || RectTransformUtility.RectangleContainsScreenPoint(menuPorts[i], screen, null)) return i;
        return -1;
    }
    void AnimateHero()
    {
        phase += Time.unscaledDeltaTime / 1.65f;
        float t = phase % 1, leap = Mathf.Sin(Mathf.Clamp01((t-.15f)/.7f) * Mathf.PI);
        hero.localPosition = new Vector3(0, .13f + leap * 1.8f, 0);
        float squash = t < .15f ? Mathf.Sin(t/.15f*Mathf.PI)*.18f : t > .85f ? Mathf.Sin((t-.85f)/.15f*Mathf.PI)*.14f : -leap*.1f;
        hero.localScale = new Vector3(1+squash,1-squash,1+squash)*1.55f;
        mascot.leftArmPivot.localRotation = Quaternion.Euler(0,0,-25 - leap*35);
        mascot.rightArmPivot.localRotation = Quaternion.Euler(0,0,25 + leap*35);
        mascot.bodyGroup.localRotation = Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*2)*7);
        for (int i=0;i<mascot.legPivots.Count;i++) mascot.legPivots[i].localRotation = Quaternion.Euler(leap*(i%2==0 ? 32 : -32),0,0);
    }
    void ArmWire() { wiring = true; wire.enabled = true; instruction.text = "Choose an option. Esc cancels."; }
    void CancelWire() { wiring = false; wire.enabled = false; instruction.text = "Drag from Start to an option."; }
    void DrawWire(Vector3 end)
    {
        Vector3 a = PortWorld(0) - stageCamera.transform.forward*.4f;
        Vector3 b = end - stageCamera.transform.forward*.4f;
        for (int i=0;i<48;i++) { float t=i/47f; wire.SetPosition(i,Vector3.Lerp(a,b,t)+stageCamera.transform.up*Mathf.Sin(t*Mathf.PI)*.35f); }
    }
    IEnumerator Connect(int action)
    {
        if (busy || !wiring || action < 1 || action > 3) yield break;
        busy = true; DrawWire(PortWorld(action));
        instruction.text = (action == 1 ? "New game" : action == 2 ? "Levels" : "Exit");
        yield return new WaitForSecondsRealtime(.35f);
        CancelWire(); busy = false;
        if (action == 1) { BeginOrResume(); LevelManager.Instance.StartNewCampaign(); }
        else if (action == 2) { RefreshLevels(); SetPanel(levelPanel, true); }
        else ExitGame();
    }
    public void OpenMenu()
    {
        if (!IsOpen || !hasPlayed) savedTimeScale = Time.timeScale > 0 ? Time.timeScale : 1;
        Time.timeScale = 0; menuRoot.SetActive(true); stage.SetActive(true);
        levelPanel.SetActive(false); settingsPanel.SetActive(false); menuButton.gameObject.SetActive(false);
        // LevelManager already loads the saved available level before the title opens.
        resumeButton.gameObject.SetActive(true); resumeLabel.text = "Resume";
        CancelWire(); EventSystem.current?.SetSelectedGameObject(null);
    }
    public void BeginOrResume()
    {
        hasPlayed = true; CancelWire(); menuRoot.SetActive(false); stage.SetActive(false);
        menuButton.gameObject.SetActive(true); Time.timeScale = savedTimeScale;
        if (GameManager.Instance != null) GameManager.Instance.currentState = GameState.Playing;
        EventSystem.current?.SetSelectedGameObject(null); GameVolume.Save();
    }
    void SetPanel(GameObject panel, bool open)
    {
        CancelWire(); panel.SetActive(open);
        if (open) EventSystem.current?.SetSelectedGameObject(panel.GetComponentInChildren<Button>().gameObject);
        else EventSystem.current?.SetSelectedGameObject(null);
    }
    void BuildLevels()
    {
        levelPanel = Modal("Campaign", "Levels"); RefreshLevels();
    }
    void RefreshLevels()
    {
        Transform old = levelPanel.transform.Find("Levels"); if (old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
        var list = new GameObject("Levels", typeof(RectTransform)).GetComponent<RectTransform>(); list.SetParent(levelPanel.transform,false); Stretch(list);
        var lm = LevelManager.Instance; lm.RestoreProgress();
        for (int i=0;i<lm.campaignLevels.Count;i++)
        {
            int index=i; bool available = lm.IsLevelSolved(i) || i == lm.HighestUnlockedLevel-1;
            string label = (i+1).ToString("00") + "   " + UIManager.DisplayTitle(lm.campaignLevels[i].title, i) + (lm.IsLevelSolved(i) ? "  ✓" : available ? "" : "  · Locked");
            var b = ButtonAt(list,label,new Vector2(.23f+(i%2)*.29f,.72f-(i/2)*.105f),new Vector2(520,76),()=>
            {
                // Close the display stage before registering the new gameplay board.
                BeginOrResume(); if (!lm.SelectAvailableLevel(index)) OpenMenu();
            });
            b.interactable=available;
        }
    }
    GameObject Modal(string name, string title)
    {
        var p = new GameObject(name,typeof(RectTransform),typeof(Image)); p.transform.SetParent(safeRoot,false); Stretch((RectTransform)p.transform);
        p.GetComponent<Image>().color = new Color(.025f,.055f,.09f,.98f);
        Label(p.transform,title,new Vector2(.23f,.86f),new Vector2(1000,70),44,Color.white);
        ButtonAt(p.transform,"← Back",new Vector2(.06f,.94f),new Vector2(200,55),()=>SetPanel(p,false));
        p.SetActive(false); return p;
    }
    void BuildSettings()
    {
        settingsPanel = Modal("Audio settings","Audio");
        VolumeRow("Music",.65f,GameVolume.Music,GameVolume.SetMusic);
        VolumeRow("Sound effects",.47f,GameVolume.Sfx,GameVolume.SetSfx);
    }
    void VolumeRow(string name,float y,float value,UnityEngine.Events.UnityAction<float> change)
    {
        var label = Label(settingsPanel.transform,name+"   "+Mathf.RoundToInt(value*100)+"%",new Vector2(.25f,y+.06f),new Vector2(1000,50),30,Ice);
        var root = new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Slider)); root.transform.SetParent(settingsPanel.transform,false);
        Place((RectTransform)root.transform,new Vector2(.25f,y),new Vector2(900,28)); root.GetComponent<Image>().color=new Color(.1f,.23f,.28f);
        var handle=new GameObject("Handle",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>(); handle.SetParent(root.transform,false); handle.sizeDelta=new Vector2(32,48);
        handle.GetComponent<Image>().color=Gold;
        var slider=root.GetComponent<Slider>(); slider.handleRect=handle; slider.targetGraphic=handle.GetComponent<Image>(); slider.value=value;
        slider.onValueChanged.AddListener(v=> { change(v); label.text=name+"   "+Mathf.RoundToInt(v*100)+"%"; });
    }
    // Game title: bold white "JOYSTIX" with "BOT" in the ice accent, an ink edge and soft
    // ice glow so it reads over the dusk sky, underlined by a short gold rule.
    void BuildTitle()
    {
        var title = Label(safeRoot, "JOYSTIX <color=#45E6F2>BOT</color>", new Vector2(.06f, .93f), new Vector2(1000, 120), 96, Color.white);
        title.name = "JOYSTIX BOT";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 6f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.alignment = TextAlignmentOptions.BottomLeft;
        Material mat = title.fontMaterial; // Per-title instance; the shared font material is untouched.
        title.outlineWidth = .12f;
        title.outlineColor = Ink;
        mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(Ice.r, Ice.g, Ice.b, .35f));
        mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, .4f);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, .6f);

        var rule = new GameObject("Title gold rule", typeof(RectTransform), typeof(Image));
        rule.transform.SetParent(safeRoot, false);
        Place((RectTransform)rule.transform, new Vector2(.062f, .81f), new Vector2(260, 5));
        var ruleImage = rule.GetComponent<Image>();
        ruleImage.color = Gold;
        ruleImage.raycastTarget = false;
    }
    static TextMeshProUGUI Label(Transform parent,string value,Vector2 anchor,Vector2 size,float fontSize,Color color)
    {
        var text=new GameObject(value,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent,false); Place(text.rectTransform,anchor,size); text.text=value; text.fontSize=fontSize; text.color=color;
        text.raycastTarget=false; text.textWrappingMode=TextWrappingModes.Normal; return text;
    }
    static Button ButtonAt(Transform parent,string value,Vector2 anchor,Vector2 size,UnityEngine.Events.UnityAction action)
    {
        var go=new GameObject(value,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); Place((RectTransform)go.transform,anchor,size);
        go.GetComponent<Image>().color=new Color(.09f,.21f,.27f); var b=go.GetComponent<Button>(); b.onClick.AddListener(action);
        var colors=b.colors; colors.highlightedColor=Ice; colors.selectedColor=Ice; colors.disabledColor=new Color(.4f,.4f,.4f,.6f); b.colors=colors;
        var text=Label(go.transform,value,new Vector2(0,1),size,23,Color.white); text.alignment=TextAlignmentOptions.Center; return b;
    }
    static void Place(RectTransform rect,Vector2 anchor,Vector2 size) { rect.anchorMin=rect.anchorMax=anchor; rect.pivot=new Vector2(0,1); rect.anchoredPosition=Vector2.zero; rect.sizeDelta=size; }
    static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
    static void SetLayer(Transform root) { root.gameObject.layer=StageLayer; foreach(Transform child in root) SetLayer(child); }
    void ExitGame()
    {
        GameVolume.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
    void OnDestroy()
    {
        if (Instance != this) return;
        Time.timeScale=savedTimeScale; Instance=null;
        if (stage != null) Destroy(stage);
        if (stageTexture != null) { stageTexture.Release(); Destroy(stageTexture); }
        foreach(var mat in ownedMaterials) if(mat != null) Destroy(mat);
        foreach(var pair in sceneLightMasks) if(pair.Key != null) pair.Key.cullingMask = pair.Value;
    }
}

// Rectilinear action glyphs inside gameplay-style cards; no circular button containers.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class TitleNodeGlyph : MaskableGraphic
{
    public int symbol;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (symbol == 1) Triangle(vh, new Vector2(-.22f,-.32f), new Vector2(-.22f,.32f), new Vector2(.32f,0));
        else if (symbol == 2)
        {
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2) Box(vh, new Vector2(x*.2f,y*.2f), new Vector2(.28f,.28f));
        }
        else
        {
            Box(vh, new Vector2(-.26f,0), new Vector2(.07f,.7f));
            Box(vh, new Vector2(-.08f,.32f), new Vector2(.4f,.07f));
            Box(vh, new Vector2(-.08f,-.32f), new Vector2(.4f,.07f));
            Box(vh, new Vector2(-.14f,-.03f), new Vector2(.2f,.53f));
            Box(vh, new Vector2(.1f,0), new Vector2(.38f,.09f));
            Triangle(vh, new Vector2(.22f,-.19f), new Vector2(.22f,.19f), new Vector2(.42f,0));
        }
    }
    Vector3 Point(Vector2 p) { var r = rectTransform.rect; return new Vector3(r.center.x+p.x*r.width,r.center.y+p.y*r.height,0); }
    void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c)
    {
        int n=vh.currentVertCount; vh.AddVert(Point(a),color,Vector2.zero); vh.AddVert(Point(b),color,Vector2.zero); vh.AddVert(Point(c),color,Vector2.zero); vh.AddTriangle(n,n+1,n+2);
    }
    void Box(VertexHelper vh, Vector2 center, Vector2 size)
    {
        Vector2 min=center-size*.5f,max=center+size*.5f;
        Triangle(vh,new Vector2(min.x,min.y),new Vector2(min.x,max.y),new Vector2(max.x,max.y));
        Triangle(vh,new Vector2(min.x,min.y),new Vector2(max.x,max.y),new Vector2(max.x,min.y));
    }
}
