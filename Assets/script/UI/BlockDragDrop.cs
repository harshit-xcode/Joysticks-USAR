using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Palette blocks support both drag/drop and click-to-add.
public class BlockDragDrop : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public BlockType blockType;
    [Tooltip("Optional. A simple visual is created automatically when this is empty.")]
    public GameObject dragIconPrefab;

    GameObject dragIcon;
    Canvas rootCanvas;
    CanvasGroup canvasGroup;
    float originalAlpha;
    bool originalBlocksRaycasts;
    bool dragging;

    void Awake()
    {
        var canvas = GetComponentInParent<Canvas>();
        rootCanvas = canvas != null ? canvas.rootCanvas : null;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || dragging || eventData.dragging) return;
        if (UIManager.Instance != null) UIManager.Instance.TryAddBlock(blockType);
        else Debug.LogWarning("BlockDragDrop: Add an active UIManager to the scene.", this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ProgramSequence.IsBoundary(blockType)) return;
        if (eventData.button != PointerEventData.InputButton.Left || rootCanvas == null) return;
        var executor = UIManager.Instance != null ? UIManager.Instance.codeExecutor : null;
        if (executor != null && executor.IsRunning()) return;
        dragging = true;
        if (dragIconPrefab != null)
            dragIcon = Instantiate(dragIconPrefab, rootCanvas.transform);
        else
        {
            dragIcon = new GameObject("Dragging " + blockType, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dragIcon.transform.SetParent(rootCanvas.transform, false);
            var image = dragIcon.GetComponent<Image>();
            var source = GetComponent<Image>();
            image.color = source != null ? source.color : new Color(.2f, .5f, .9f, .85f);
            image.sprite = source != null ? source.sprite : null;
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            labelObject.transform.SetParent(dragIcon.transform, false);
            var label = labelObject.GetComponent<TMPro.TextMeshProUGUI>();
            label.text = CommandLabels.For(blockType);
            label.fontSize = 22;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            var textRect = label.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        }
        dragIcon.transform.SetAsLastSibling();
        var rect = dragIcon.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            var sourceRect = transform as RectTransform;
            rect.sizeDelta = sourceRect != null ? sourceRect.rect.size : new Vector2(150, 50);
            rect.localScale = Vector3.one;
        }
        // The ghost must never intercept a drop intended for the slot underneath it.
        foreach (var graphic in dragIcon.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        var ghostGroup = dragIcon.GetComponent<CanvasGroup>();
        if (ghostGroup == null) ghostGroup = dragIcon.AddComponent<CanvasGroup>();
        ghostGroup.blocksRaycasts = false;
        ghostGroup.interactable = false;
        foreach (var handler in dragIcon.GetComponentsInChildren<BlockDragDrop>(true)) handler.enabled = false;
        if (canvasGroup != null)
        {
            originalAlpha = canvasGroup.alpha;
            originalBlocksRaycasts = canvasGroup.blocksRaycasts;
            canvasGroup.alpha = .5f;
            canvasGroup.blocksRaycasts = false;
        }
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || dragIcon == null) return;
        var rect = dragIcon.GetComponent<RectTransform>();
        if (rect == null) return;
        var camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rootCanvas.transform as RectTransform,
            eventData.position, camera, out var worldPoint)) rect.position = worldPoint;
    }

    public void OnEndDrag(PointerEventData eventData) { EndDrag(); }
    void OnDisable() { EndDrag(); }

    void EndDrag()
    {
        if (dragIcon != null) Destroy(dragIcon);
        dragIcon = null;
        if (dragging && canvasGroup != null)
        {
            canvasGroup.alpha = originalAlpha;
            canvasGroup.blocksRaycasts = originalBlocksRaycasts;
        }
        dragging = false;
    }
}
