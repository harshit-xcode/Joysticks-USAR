using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ProgramDropTarget : MonoBehaviour, IDropHandler
{
    public UIManager owner;
    public void OnDrop(PointerEventData eventData)
    {
        var block = eventData.pointerDrag != null ? eventData.pointerDrag.GetComponentInParent<BlockDragDrop>() : null;
        if (block != null && owner != null) owner.TryAddBlock(block.blockType);
    }
}
