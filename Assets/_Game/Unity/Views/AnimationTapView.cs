using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    [RequireComponent(typeof(Image))]
    public class AnimationTapView : MonoBehaviour, IPointerDownHandler, ICanvasRaycastFilter
    {
        private Func<bool> m_CaptureFilter;
        public event Action Tapped;

        public void SetCaptureFilter(Func<bool> filter)
        {
            m_CaptureFilter = filter;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return isActiveAndEnabled && m_CaptureFilter != null && m_CaptureFilter();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left &&
                IsRaycastLocationValid(eventData.position, eventData.pressEventCamera))
                Tapped?.Invoke();
        }
    }
}
