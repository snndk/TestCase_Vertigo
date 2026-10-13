using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class WalletFeedbackView : MonoBehaviour
    {
        [SerializeField] private TMP_Text balance;
        [SerializeField] private TMP_Text spending;
        [SerializeField] private RectTransform motion;
        [SerializeField, Min(0.01f)] private float countDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float feedbackDuration = 0.95f;
        [SerializeField, Min(1f)] private float pulseScale = 1.1f;
        [SerializeField, Min(0f)] private float riseDistance = 20f;
        private Sequence m_Animation;
        private long m_Balance;
        private bool m_Initialized;
        private Vector2 m_SpendingOrigin;
        private bool m_HasOrigin;

        public void ResetBalance()
        {
            StopFeedback();
            m_Initialized = false;
        }

        public void ShowBalance(long amount)
        {
            if (m_Initialized && m_Balance == amount)
                return;
            StopFeedback();
            m_Initialized = true;
            m_Balance = amount;
            balance.text = amount.ToString("N0");
        }

        public void ShowSpending(long amount, string currencyName)
        {
            if (!m_Initialized || amount <= 0 || !isActiveAndEnabled)
                return;

            StopFeedback();
            var target = m_Balance;
            var start = checked(target + amount);
            balance.text = start.ToString("N0");
            spending.text = $"−{amount:N0} {currencyName}";
            spending.alpha = 1f;
            m_Animation = DOTween.Sequence().SetUpdate(true);
            AddBalanceCountdown(m_Animation, target, amount);
            AddWalletPulse(m_Animation);
            AddSpendingFloat(m_Animation);
            m_Animation.OnComplete(CompleteFeedback);
        }

        private void AddBalanceCountdown(Sequence animation, long target, long amount)
        {
            var progress = 0f;
            animation.Append(DOTween.To(() => progress, value =>
            {
                progress = value;
                // Decimal interpolation preserves long balances without float precision loss.
                var displayed = target + (long)Math.Round(amount * (1m - (decimal)value));
                balance.text = displayed.ToString("N0");
            }, 1f, countDuration).SetEase(Ease.OutCubic));
        }

        private void AddWalletPulse(Sequence animation)
        {
            animation.Insert(0, motion.DOScale(pulseScale, 0.12f).SetEase(Ease.OutQuad));
            animation.Insert(0.12f, motion.DOScale(1f, 0.25f).SetEase(Ease.OutSine));
        }

        private void AddSpendingFloat(Sequence animation)
        {
            animation.Insert(0, DOTween.To(() => spending.rectTransform.anchoredPosition,
                value => spending.rectTransform.anchoredPosition = value,
                m_SpendingOrigin + Vector2.up * riseDistance, feedbackDuration).SetEase(Ease.OutQuad));
            animation.Insert(feedbackDuration * 0.4f,
                DOTween.To(() => spending.alpha, value => spending.alpha = value, 0f, feedbackDuration * 0.6f));
        }

        private void CompleteFeedback()
        {
            m_Animation = null;
            RestoreVisuals();
        }

        private void StopFeedback()
        {
            m_Animation?.Kill(false);
            m_Animation = null;
            RestoreVisuals();
        }

        private void RestoreVisuals()
        {
            if (!m_HasOrigin)
            {
                m_SpendingOrigin = spending.rectTransform.anchoredPosition;
                m_HasOrigin = true;
            }
            motion.localScale = Vector3.one;
            spending.alpha = 0;
            spending.rectTransform.anchoredPosition = m_SpendingOrigin;
            if (m_Initialized)
                balance.text = m_Balance.ToString("N0");
        }

        private void OnDisable() => StopFeedback();
    }
}
