using UnityEngine;
using UnityEngine.EventSystems;

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
