using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class RewardRowFeedback : MonoBehaviour
    {
        [SerializeField] private RectTransform motion;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text amount;
        [SerializeField] private TMP_Text gain;
        [SerializeField, Min(0.01f)] private float gainDuration = 0.65f;
        [SerializeField, Min(0f)] private float entranceOffset = 24f;
        private const float m_IconPulseScale = 1.18f;
        private const float m_IconPulseRiseDuration = 0.05f;
        private const float m_IconPulseReturnDuration = 0.12f;
        private const float m_GainRiseDistance = 18f;
        private const float m_GainFadeStart = 0.35f;
        // The outcome sequence owns entrance/count playback. This component owns local pulse/gain playback.
        private Tween m_IconPulseTween;
        private RectTransform m_PulsingIcon;
        private Vector3 m_IconRestScale;
        private Tween m_CountTween;
        private Tween m_GainTween;
        private long m_Target;
        private bool m_HasTotal;
        private Vector2 m_GainOrigin;

        private void Awake() => m_GainOrigin = gain.rectTransform.anchoredPosition;

        public Tween CreateEntranceTween(float duration)
        {
            var progress = 0f;
            group.alpha = 0f;
            motion.anchoredPosition = Vector2.right * entranceOffset;
            return DOTween.To(() => progress, value =>
            {
                progress = value;
                if (!isActiveAndEnabled)
                    return;
                group.alpha = value;
                motion.anchoredPosition = Vector2.right * (entranceOffset * (1f - value));
            }, 1f, duration).SetEase(Ease.OutCubic);
        }

        public void FinishEntrance()
        {
            group.alpha = 1f;
            motion.anchoredPosition = Vector2.zero;
        }

        public void PulseIcon(RectTransform icon)
        {
            if (m_PulsingIcon != icon)
            {
                ResetIconPulse();
                m_PulsingIcon = icon;
                m_IconRestScale = icon.localScale;
            }
            m_IconPulseTween?.Kill(false);
            m_IconPulseTween = DOTween.Sequence()
                .Append(icon.DOScale(m_IconRestScale * m_IconPulseScale, m_IconPulseRiseDuration)
                    .SetEase(Ease.OutQuad))
                .Append(icon.DOScale(m_IconRestScale, m_IconPulseReturnDuration).SetEase(Ease.OutBack))
                .SetUpdate(true);
        }

        private void ResetIconPulse()
        {
            m_IconPulseTween?.Kill(false);
            m_IconPulseTween = null;
            if (m_PulsingIcon != null)
                m_PulsingIcon.localScale = m_IconRestScale;
            m_PulsingIcon = null;
        }

        public void ResetArrival()
        {
            ResetIconPulse();
            FinishEntrance();
        }

        public void ShowTotal(long total)
        {
            m_CountTween?.Kill(false);
            m_Target = total;
            m_HasTotal = true;
            amount.text = total.ToString("N0");
        }

        public Tween CreateCountTween(long previous, long total, float duration)
        {
            ShowTotal(total);
            amount.text = previous.ToString("N0");
            var progress = 0f;
            m_CountTween = DOTween.To(() => progress, value =>
            {
                progress = value;
                // Decimal interpolation preserves integer totals even beyond float's exact range.
                var displayed = (long)(previous + ((decimal)total - previous) * (decimal)value);
                amount.text = displayed.ToString("N0");
            }, 1f, duration).SetEase(Ease.Linear)
                .OnStart(() => PlayGain(total - previous));
            return m_CountTween;
        }

        private void PlayGain(long delta)
        {
            m_GainTween?.Kill(false);
            gain.text = "+" + delta.ToString("N0");
            gain.rectTransform.anchoredPosition = m_GainOrigin;
            gain.alpha = 1f;
            gain.gameObject.SetActive(true);
            var progress = 0f;
            m_GainTween = DOTween.To(() => progress, value =>
            {
                progress = value;
                gain.rectTransform.anchoredPosition = m_GainOrigin + Vector2.up * (m_GainRiseDistance * value);
                gain.alpha = 1f - Mathf.InverseLerp(m_GainFadeStart, 1f, value);
            }, 1f, gainDuration).SetEase(Ease.OutQuad).SetUpdate(true)
                .OnComplete(() => gain.gameObject.SetActive(false));
        }

        public void ResetVisuals()
        {
            m_CountTween?.Kill(false);
            m_GainTween?.Kill(false);
            m_CountTween = null;
            m_GainTween = null;
            ResetArrival();
            gain.gameObject.SetActive(false);
            if (m_HasTotal)
                amount.text = m_Target.ToString("N0");
        }

        private void OnDisable() => ResetVisuals();
    }
}
