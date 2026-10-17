using UnityEngine;
using UnityEngine.EventSystems;

// Right-click is a shortcut for the row's visible remove button.
public sealed class ProgramCommandRow : MonoBehaviour, IPointerClickHandler
{
    public UIManager owner;
    public int commandIndex;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && owner != null)
            owner.RemoveCommandAt(commandIndex);
    }
}
