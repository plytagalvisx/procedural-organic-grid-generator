using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class DetectMouse : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static bool enterUI;
    public void OnPointerEnter(PointerEventData eventData)
    {
        enterUI = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        enterUI = false;
    }
}
