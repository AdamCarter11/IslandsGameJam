using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Lightweight runtime hover hook for UI that is not prefab-authored with hover handlers.
/// </summary>
public class UiPointerHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Action onEnter;
    Action onExit;

    public void SetHandlers(Action enter, Action exit)
    {
        onEnter = enter;
        onExit = exit;
    }

    public void OnPointerEnter(PointerEventData eventData) => onEnter?.Invoke();

    public void OnPointerExit(PointerEventData eventData) => onExit?.Invoke();
}
