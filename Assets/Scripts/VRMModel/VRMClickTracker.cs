using UnityEngine;
using UnityEngine.EventSystems;
public class VRMClickTracker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static bool IsVRMClicked { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsVRMClicked = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsVRMClicked = false;
    }
}