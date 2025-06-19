using UnityEngine;
using UnityEngine.EventSystems;
using Kirurobo;

public class WindowDragger : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    private Vector2 pointerDownPos;
    private Vector2 offset;
    private UniWindowController window;

    void Start()
    {
        window = GameObject.FindAnyObjectByType<UniWindowController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos = eventData.position;
        if (window != null)
        {
            offset = window.windowPosition - window.cursorPosition;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (window == null) return;

        // マウスカーソルの現在位置に、最初に計算したオフセットを加えてウィンドウを移動する
        window.windowPosition = window.cursorPosition + offset;
    }
}
