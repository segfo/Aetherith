using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickUtil : MonoBehaviour, IPointerClickHandler
{
    public event Action SingleClick;
    public event Action DoubleClick;
    bool isClick;

    public void OnClick()
    {
        if (!isClick)
        {
            isClick = true;
            StartCoroutine(MeasureTime());
            SingleClick?.Invoke();
        }
        else
        {
            DoubleClick?.Invoke();
            isClick = false;
        }
    }

    IEnumerator MeasureTime()
    {
        float times = 0f;
        while (isClick)
        {
            times += Time.deltaTime;
            if (times < AppConfigManager.Instance.Config.doubleClickThreshold)
            {
                yield return null;
            }
            else
            {
                isClick = false;
                yield break;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick();
    }
}
