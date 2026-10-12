using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    [RequireComponent(typeof(Image))]
    public class AnimationTapView : MonoBehaviour, IPointerDownHandler, ICanvasRaycastFilter
    {
        [SerializeField] private WheelView wheel;

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return isActiveAndEnabled && wheel != null && (wheel.InputBlocked || wheel.CanAccelerate);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (wheel != null && !wheel.InputBlocked && eventData.button == PointerEventData.InputButton.Left)
                wheel.Accelerate();
        }
    }
}
