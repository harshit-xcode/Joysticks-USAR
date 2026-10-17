using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlockSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public BlockType? placedBlockType = null;
    public Image slotBackground;
    public TMPro.TextMeshProUGUI slotLabel;
    public Color emptyColour = new Color(.8f, .8f, .8f, .5f);
    public Color filledColour = new Color(.3f, .8f, .3f, 1);

    void Awake()
    {
        if (slotBackground == null) slotBackground = GetComponent<Image>();
        if (slotLabel == null) slotLabel = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (slotLabel == null && transform is RectTransform)
        {
            var child = new GameObject("Slot label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            child.transform.SetParent(transform, false);
            slotLabel = child.GetComponent<TMPro.TextMeshProUGUI>();
            slotLabel.fontSize = 22;
            slotLabel.alignment = TMPro.TextAlignmentOptions.Center;
            slotLabel.color = Color.black;
            slotLabel.rectTransform.anchorMin = Vector2.zero;
            slotLabel.rectTransform.anchorMax = Vector2.one;
            slotLabel.rectTransform.offsetMin = slotLabel.rectTransform.offsetMax = Vector2.zero;
        }
        if (slotLabel != null) slotLabel.raycastTarget = false;
        RefreshVisual();
    }

    bool EditingAllowed()
    {
        var executor = UIManager.Instance != null ? UIManager.Instance.codeExecutor : null;
        return executor == null || !executor.IsRunning();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!EditingAllowed()) return;
        var block = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponentInParent<BlockDragDrop>() : null;
        if (block != null) PlaceBlock(block.blockType);
    }

    // Right-click removes a placed command.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && EditingAllowed()) ClearSlot();
    }

    public void PlaceBlock(BlockType type)
    {
        if (ProgramSequence.IsBoundary(type)) return;
        // Existing scenes may have duplicate BlockSlot components on one panel.
        // Keep them synchronized so UIManager reads the same state as OnDrop writes.
        foreach (var slot in GetComponents<BlockSlot>())
        {
            slot.placedBlockType = type;
            slot.RefreshVisual();
        }
    }

    public void ClearSlot()
    {
        foreach (var slot in GetComponents<BlockSlot>())
        {
            slot.placedBlockType = null;
            slot.RefreshVisual();
        }
    }

    public bool IsOccupied() { return placedBlockType.HasValue; }

    void RefreshVisual()
    {
        if (slotBackground != null) slotBackground.color = IsOccupied() ? filledColour : emptyColour;
        if (slotLabel != null) slotLabel.text = IsOccupied() ? CommandLabels.For(placedBlockType.Value) : "Empty";
    }
}
