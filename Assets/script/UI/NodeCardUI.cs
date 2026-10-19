// NodeCardUI.cs
// Interactive visual component for a Node Card in either NODE MODIFIERS (palette)
// or MACHINE LEARNING LAB (node graph workspace), plus NodePortUI for port-to-port wiring.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class NodeCardUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Node Identity")]
    public BlockType blockType;
    public bool isPaletteCard = false;
    public bool isFixedEndpoint = false;
    public int paletteSlotIndex = -1;

    [Header("Graph Connections (Runtime)")]
    public NodeCardUI connectedNext;
    public NodeCardUI connectedPrev;

    [Header("Visual Elements")]
    public RectTransform rectTransform;
    public Image cardBackground;
    public Image ledIndicator;
    public Image actionIcon;
    public TextMeshProUGUI titleLabel;
    public RectTransform inputPortRect;
    public RectTransform outputPortRect;
    public Image inputPortImage;
    public Image outputPortImage;
    public Image highlightBorder;

    private Canvas rootCanvas;
    private GameObject dragGhost;
    private Vector2 dragPointerOffset;
    private bool isDraggingCard;

    private void Awake()
    {
        if (rectTransform == null) rectTransform = transform as RectTransform;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null) rootCanvas = canvas.rootCanvas;
    }

    public void SetExecutionHighlight(bool active)
    {
        if (highlightBorder != null)
            highlightBorder.enabled = active;
        if (ledIndicator != null)
        {
            ledIndicator.color = active
                ? new Color(0.45f, 1.0f, 0.35f, 1f)
                : CommandLabels.LedColorFor(blockType);
        }
        if (rectTransform != null)
            rectTransform.localScale = active ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDraggingCard || eventData.dragging) return;
        if (UIManager.Instance == null || !UIManager.Instance.IsEditingAllowed) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isPaletteCard)
            {
                UIManager.Instance.QuickPlaceFromPalette(this);
            }
            else
            {
                UIManager.Instance.OnNodeCardClicked(this);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!isPaletteCard && !isFixedEndpoint)
            {
                UIManager.Instance.RemoveGraphNode(this);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (UIManager.Instance == null || !UIManager.Instance.IsEditingAllowed) return;

        if (rootCanvas == null)
        {
            var c = GetComponentInParent<Canvas>();
            if (c != null) rootCanvas = c.rootCanvas;
        }
        if (rootCanvas == null) return;

        isDraggingCard = true;

        if (isPaletteCard)
        {
            // Spawn floating drag ghost while dragging from NODE MODIFIERS
            dragGhost = Instantiate(gameObject, rootCanvas.transform);
            dragGhost.name = "Ghost_" + blockType;
            var ghostCard = dragGhost.GetComponent<NodeCardUI>();
            if (ghostCard != null) ghostCard.enabled = false;
            foreach (var g in dragGhost.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
            var gr = dragGhost.GetComponent<RectTransform>();
            gr.anchorMin = gr.anchorMax = new Vector2(0.5f, 0.5f);
            gr.pivot = new Vector2(0.5f, 0.5f);
            gr.sizeDelta = new Vector2(104f, 104f);
            gr.localScale = new Vector3(1.05f, 1.05f, 1f);
            UpdateGhostPosition(eventData);
        }
        else
        {
            transform.SetAsLastSibling();
            RectTransform parentRect = rectTransform.parent as RectTransform;
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, cam, out Vector2 localPt))
            {
                dragPointerOffset = rectTransform.anchoredPosition - localPt;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingCard) return;

        if (isPaletteCard)
        {
            UpdateGhostPosition(eventData);
        }
        else
        {
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null) return;
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, cam, out Vector2 localPt))
            {
                Vector2 target = localPt + dragPointerOffset;
                Rect bounds = parentRect.rect;
                float pad = 62f;
                target.x = Mathf.Clamp(target.x, bounds.xMin + pad, bounds.xMax - pad);
                target.y = Mathf.Clamp(target.y, bounds.yMin + pad, bounds.yMax - pad);
                rectTransform.anchoredPosition = target;
                UIManager.Instance?.RefreshWires();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingCard) return;
        isDraggingCard = false;

        if (isPaletteCard)
        {
            if (dragGhost != null) Destroy(dragGhost);
            dragGhost = null;
            UIManager.Instance?.TryDropPaletteNodeOnLab(this, eventData.position);
        }
        else
        {
            UIManager.Instance?.OnGraphNodeDragEnded(this, eventData.position);
        }
    }

    private void UpdateGhostPosition(PointerEventData eventData)
    {
        if (dragGhost == null || rootCanvas == null) return;
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rootCanvas.transform as RectTransform, eventData.position, cam, out Vector3 worldPt))
        {
            dragGhost.transform.position = worldPt;
        }
    }

    private void OnDisable()
    {
        if (dragGhost != null) Destroy(dragGhost);
        dragGhost = null;
        isDraggingCard = false;
    }
}

public class NodePortUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    public NodeCardUI ownerNode;
    public bool isOutputPort;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (ownerNode == null || ownerNode.isPaletteCard) return;
        UIManager.Instance?.BeginPortWireDrag(this, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ownerNode == null || ownerNode.isPaletteCard) return;
        UIManager.Instance?.UpdatePortWireDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ownerNode == null || ownerNode.isPaletteCard) return;
        UIManager.Instance?.EndPortWireDrag(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (ownerNode == null || ownerNode.isPaletteCard) return;
        UIManager.Instance?.CompletePortWireDrop(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ownerNode == null || ownerNode.isPaletteCard) return;
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            UIManager.Instance?.DisconnectPort(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            UIManager.Instance?.OnPortClicked(this);
        }
    }
}
