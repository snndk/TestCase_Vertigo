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
        [SerializeField] private WheelEntranceCelebration celebration;
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
            m_Sequence = DOTween.Sequence().SetUpdate(true);
            try
            {
                tier.Animation.BuildEntrance(m_Sequence, this, tier);
                m_Sequence.OnComplete(Finish);
            }
            catch
            {
                Finish();
                throw;
            }
        }

        public void AppendTransition(Sequence sequence, WheelTierAsset tier,
            float expansionMultiplier = 1f, float extraOvershoot = 0f)
        {
            PrepareLight(tier);
            sequence.Append(motion.DOScale(m_RestScale * shrinkScale, shrinkDuration).SetEase(Ease.InQuad));
            sequence.AppendCallback(ApplyAppearance);
            var expansionDuration = expandDuration * expansionMultiplier;
            sequence.Append(motion.DOScale(m_RestScale, expansionDuration)
                .SetEase(Ease.OutBack, expandOvershoot + extraOvershoot));
            AddLight(sequence, tier, expansionDuration);
        }

        public Tween CreateCelebration(string message, Color color, float strength)
        {
            return celebration.Create(message, color, strength);
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

        private void AddLight(Sequence sequence, WheelTierAsset tier, float duration)
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
            }, 1f, duration).SetEase(Ease.Linear));
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
            if (celebration != null)
                celebration.ResetVisuals();
        }

        private void OnDisable() => Finish();
    }
}
