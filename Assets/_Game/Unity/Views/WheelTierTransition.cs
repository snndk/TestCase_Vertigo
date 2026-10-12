using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Unity._Game.Unity.Authoring;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class WheelTierTransition : MonoBehaviour
    {
        [SerializeField] private RectTransform motion;
        [SerializeField] private Image glow;
        [SerializeField] private Image[] sparkles;
        [SerializeField, Min(0.01f)] private float shrinkDuration = 0.14f;
        [SerializeField, Min(0.01f)] private float expandDuration = 0.31f;
        [SerializeField, Range(0.5f, 1f)] private float shrinkScale = 0.92f;
        [SerializeField, Min(0f)] private float expandOvershoot = 1.2f;
        private const float m_GlowStartScale = 0.85f;
        private const float m_GlowEndScale = 1.12f;
        private const float m_SparkleStartRadius = 0.43f;
        private const float m_SparkleEndRadius = 0.62f;
        private Sequence m_Sequence;
        private Action m_ApplyAppearance;
        private Vector3 m_RestScale;
        public bool IsPlaying => m_Sequence != null && m_Sequence.IsActive();

        public void Play(WheelTierAsset tier, Action applyAppearance)
        {
            Finish();
            m_RestScale = motion.localScale;
            m_ApplyAppearance = applyAppearance;
            PrepareLight(tier);
            m_Sequence = DOTween.Sequence().SetUpdate(true);
            m_Sequence.Append(motion.DOScale(m_RestScale * shrinkScale, shrinkDuration).SetEase(Ease.InQuad));
            m_Sequence.AppendCallback(ApplyAppearance);
            m_Sequence.Append(motion.DOScale(m_RestScale, expandDuration).SetEase(Ease.OutBack, expandOvershoot));
            AddLight(m_Sequence, tier);
            m_Sequence.OnComplete(Finish);
        }

        private void PrepareLight(WheelTierAsset tier)
        {
            glow.color = new Color(tier.TransitionColor.r, tier.TransitionColor.g, tier.TransitionColor.b, 0f);
            glow.rectTransform.localScale = Vector3.one * m_GlowStartScale;
            glow.gameObject.SetActive(true);
            var count = Mathf.Min(tier.TransitionSparkles, sparkles.Length);
            for (var index = 0; index < sparkles.Length; index++)
            {
                sparkles[index].gameObject.SetActive(index < count);
                sparkles[index].color = glow.color;
            }
        }

        private void AddLight(Sequence sequence, WheelTierAsset tier)
        {
            var count = Mathf.Min(tier.TransitionSparkles, sparkles.Length);
            var progress = 0f;
            sequence.Insert(shrinkDuration, DOTween.To(() => progress, value =>
            {
                progress = value;
                var brightness = Mathf.Sin(value * Mathf.PI);
                var color = tier.TransitionColor;
                color.a = brightness * tier.TransitionGlowStrength;
                glow.color = color;
                glow.rectTransform.localScale = Vector3.one * Mathf.Lerp(m_GlowStartScale, m_GlowEndScale, value);
                UpdateSparkles(count, value, brightness, tier.TransitionColor);
            }, 1f, expandDuration).SetEase(Ease.Linear));
        }

        private void UpdateSparkles(int count, float progress, float brightness, Color color)
        {
            var wheelSize = Mathf.Min(motion.rect.width, motion.rect.height);
            var radius = wheelSize * Mathf.Lerp(m_SparkleStartRadius, m_SparkleEndRadius, progress);
            color.a = brightness;
            for (var index = 0; index < count; index++)
            {
                var angle = (index + 0.5f) * Mathf.PI * 2f / count;
                var sparkle = sparkles[index];
                sparkle.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                sparkle.rectTransform.localScale = Vector3.one * brightness;
                sparkle.color = color;
            }
        }

        private void ApplyAppearance()
        {
            var apply = m_ApplyAppearance;
            m_ApplyAppearance = null;
            apply?.Invoke();
        }

        // Interruption settles the latest visual state without invoking a gameplay command.
        public void Finish()
        {
            var sequence = m_Sequence;
            m_Sequence = null;
            sequence?.Kill(false);
            ApplyAppearance();
            if (sequence != null)
                motion.localScale = m_RestScale;
            glow.gameObject.SetActive(false);
            foreach (var sparkle in sparkles)
                sparkle.gameObject.SetActive(false);
        }

        private void OnDisable() => Finish();
    }
}
