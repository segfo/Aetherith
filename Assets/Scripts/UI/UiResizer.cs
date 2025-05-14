﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIResizer : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RectTransform targetRect;
    [SerializeField] float xMinSize = 200f;
    [SerializeField] float yMinSize = 200f;

    private Vector2 originalSize;
    private Vector2 originalMousePosition;

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, eventData.position, eventData.pressEventCamera, out originalMousePosition);
        originalSize = targetRect.sizeDelta;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 currentMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRect, eventData.position, eventData.pressEventCamera, out currentMousePosition);

        Vector2 delta = currentMousePosition - originalMousePosition;
        Vector2 newSize = originalSize + new Vector2(delta.x, -delta.y);

        newSize.x = Mathf.Max(xMinSize, newSize.x); // 最小幅
        newSize.y = Mathf.Max(yMinSize, newSize.y); // 最小高さ

        targetRect.sizeDelta = newSize;
        // ★親のLayoutGroupを再計算させる
        LayoutRebuilder.MarkLayoutForRebuild(targetRect.parent as RectTransform);
    }
}
