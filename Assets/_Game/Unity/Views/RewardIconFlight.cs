using DG.Tweening;
using UnityEngine;

namespace Vertigo.Unity._Game.Unity.Views
{
    public static class RewardIconFlight
    {
        private const float m_PullbackOvershoot = 0.8f;

        public static Tween Create(RectTransform icon, RectTransform target, float duration, float arcHeight)
        {
            var parent = icon.parent;
            var start = icon.localPosition;
            var end = parent.InverseTransformPoint(target.position);
            var control = (start + end) * 0.5f + Vector3.up * arcHeight;
            var progress = 0f;
            return DOTween.To(() => progress, value =>
            {
                progress = value;
                var remaining = 1f - value;
                icon.localPosition = remaining * remaining * start +
                    2f * remaining * value * control + value * value * end;
            }, 1f, duration).SetEase(Ease.InBack, m_PullbackOvershoot);
        }
    }
}
