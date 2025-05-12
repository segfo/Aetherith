using UnityEngine;
using UnityEngine.EventSystems;
using Kirurobo;

public class WindowDragger : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private Vector2 dragStartPos;
    private Vector2 offset;
    private UniWindowController window;

    void Start()
    {
        window = GameObject.FindAnyObjectByType<UniWindowController>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (window == null) return;
        dragStartPos = eventData.position;
        offset = window.windowPosition - window.cursorPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (window == null) return;

        Vector2 delta = eventData.position - dragStartPos;
        window.windowPosition = window.cursorPosition + offset;
    }
}
