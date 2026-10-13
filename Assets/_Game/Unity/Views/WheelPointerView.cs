using DG.Tweening;
using UnityEngine;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class WheelPointerView : MonoBehaviour
    {
        [SerializeField] private RectTransform motion;
        [SerializeField, Range(0f, 90f)] private float deflectionAngle = 38f;
        private Tween m_Tween;

        public void Tick()
        {
            m_Tween?.Kill();
            var angle = -deflectionAngle;
            motion.localRotation = Quaternion.Euler(0, 0, angle);
            m_Tween = DOTween.To(() => angle, value =>
            {
                angle = value;
                motion.localRotation = Quaternion.Euler(0, 0, value);
            }, 0, .14f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void ResetMotion()
        {
            m_Tween?.Kill();
            motion.localRotation = Quaternion.identity;
        }

        private void OnDisable() => ResetMotion();
    }
}
