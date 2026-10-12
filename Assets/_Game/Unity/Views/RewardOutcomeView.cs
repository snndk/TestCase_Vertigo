using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class RewardOutcomeView : SliceOutcomeView
    {
        [SerializeField] private Image flyingIcon;
        [SerializeField] private RewardListView rewards;
        [FormerlySerializedAs("duration")]
        [SerializeField, Min(0.01f)] private float flightDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float burstDuration = 0.35f;
        [SerializeField, Range(1, 12)] private int particleCount = 6;
        [SerializeField, Min(1f)] private float enlargedScale = 1.2f;
        [SerializeField, Range(0.01f, 1f)] private float particleScale = 0.28f;
        [SerializeField] private Vector2 spreadRadius = new Vector2(1f, 1.65f);
        [SerializeField, Min(0f)] private float burstHold = 0.1f;
        [SerializeField, Min(0f)] private float flightStagger = 0.04f;
        [SerializeField, Min(0f)] private float flightArc = 100f;
        [SerializeField] private Sprite glowSprite;
        [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0.3f, 0.65f);
        private const float m_HighlightOvershoot = 1.5f;
        private const float m_ParticleGrowthDurationRatio = 0.65f;
        private const float m_GlowStartScale = 0.8f;
        private const float m_GlowEndScale = 2.8f;
        private const float m_ParticleArcScale = 0.75f;
        private const float m_ParticleArcStep = 0.08f;
        private const float m_MainIconArcScale = 1.2f;
        private const float m_MainIconArrivalScale = 0.55f;
        private const float m_ArrivalSettleDuration = 0.18f;
        private Image m_Glow;
        private RewardRowView m_ArrivalRow;
        private Image[] m_Particles;
        private Image m_Source;
        private bool m_SourceWasEnabled;
        private Vector3 m_IconScale;
        private Tween m_Animation;

        public override bool CanPresent(RoundSlice slice) => slice is RewardSlice;
        public override bool CanAccelerate => true;

        public override void SetPlaybackSpeed(float speed)
        {
            if (m_ArrivalRow != null)
                m_ArrivalRow.SetPlaybackSpeed(speed);
        }

        public override Tween Highlight(WheelSliceView slice)
        {
            ResetVisuals();
            PrepareIcons(slice);
            var reward = ((RewardSlice)slice.Slice).Reward;
            m_ArrivalRow = rewards.PrepareArrival(reward.RewardId);
            var sequence = DOTween.Sequence();
            sequence.Append(flyingIcon.rectTransform.DOScale(m_IconScale * enlargedScale, burstDuration)
                .SetEase(Ease.OutBack, m_HighlightOvershoot));
            AddParticleBurst(sequence);
            AddGlow(sequence);
            sequence.Insert(0, rewards.RevealArrival(m_ArrivalRow));
            sequence.AppendInterval(burstHold);
            m_Animation = sequence;
            return sequence;
        }

        private void AddParticleBurst(Sequence sequence)
        {
            var center = flyingIcon.rectTransform.localPosition;
            var radius = Mathf.Max(flyingIcon.rectTransform.rect.width * m_IconScale.x,
                flyingIcon.rectTransform.rect.height * m_IconScale.y);
            foreach (var image in m_Particles)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var distance = radius * Random.Range(spreadRadius.x, spreadRadius.y);
                var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var particle = image.rectTransform;
                sequence.Insert(0, particle.DOLocalMove(center + offset, burstDuration).SetEase(Ease.OutCubic));
                sequence.Insert(0, particle.DOScale(m_IconScale * particleScale,
                    burstDuration * m_ParticleGrowthDurationRatio).SetEase(Ease.OutBack));
            }
        }

        private void AddGlow(Sequence sequence)
        {
            if (m_Glow == null)
                return;

            var transparent = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            sequence.Insert(0, m_Glow.rectTransform.DOScale(m_IconScale * m_GlowEndScale, burstDuration)
                .SetEase(Ease.OutQuad));
            sequence.Insert(0, DOTween.To(() => m_Glow.color, value => m_Glow.color = value,
                transparent, burstDuration).SetEase(Ease.InQuad));
        }

        public override Tween Play(WheelSliceView slice)
        {
            var target = m_ArrivalRow.IconRect;
            var sequence = DOTween.Sequence();
            AddParticleFlights(sequence, target);

            var mainIconDepartureTime = m_Particles.Length * flightStagger;
            AddFlight(sequence, flyingIcon, target, mainIconDepartureTime, m_MainIconArcScale);
            sequence.Insert(mainIconDepartureTime,
                flyingIcon.rectTransform.DOScale(m_IconScale * m_MainIconArrivalScale, flightDuration));

            var firstArrivalTime = flightDuration;
            var lastArrivalTime = mainIconDepartureTime + flightDuration;
            var countDuration = lastArrivalTime - firstArrivalTime;
            var reward = ((RewardSlice)slice.Slice).Reward;
            sequence.Insert(firstArrivalTime, m_ArrivalRow.CreateArrivalCount(reward.Amount, countDuration));
            sequence.AppendInterval(m_ArrivalSettleDuration);
            m_Animation = sequence;
            return sequence;
        }

        private void AddParticleFlights(Sequence sequence, RectTransform target)
        {
            for (var index = 0; index < m_Particles.Length; index++)
            {
                var departureTime = index * flightStagger;
                var arcScale = m_ParticleArcScale + index * m_ParticleArcStep;
                AddFlight(sequence, m_Particles[index], target, departureTime, arcScale);
            }
        }

        private void AddFlight(Sequence sequence, Image icon, RectTransform target, float departureTime, float arcScale)
        {
            sequence.Insert(departureTime, RewardIconFlight.Create(icon.rectTransform, target, flightDuration, flightArc * arcScale));
            sequence.InsertCallback(departureTime + flightDuration, () =>
            {
                icon.gameObject.SetActive(false);
                m_ArrivalRow.PulseArrival();
            });
        }

        public override void ResetVisuals()
        {
            var animation = m_Animation;
            m_Animation = null;
            animation?.Kill(false);
            if (m_Source != null)
                m_Source.enabled = m_SourceWasEnabled;
            m_Source = null;
            if (m_ArrivalRow != null)
                m_ArrivalRow.ResetArrival();
            m_ArrivalRow = null;
            if (m_Glow != null)
                m_Glow.gameObject.SetActive(false);
            flyingIcon.gameObject.SetActive(false);
            if (m_Particles == null)
                return;
            foreach (var particle in m_Particles)
                particle.gameObject.SetActive(false);
        }

        private void PrepareIcons(WheelSliceView slice)
        {
            EnsureEffectIcons();
            HideSourceIcon(slice);
            PrepareGlow(slice);
            ConfigureIcon(flyingIcon, slice, m_IconScale);
            foreach (var particle in m_Particles)
                ConfigureIcon(particle, slice, Vector3.zero);
        }

        private void EnsureEffectIcons()
        {
            if (m_Particles == null)
            {
                m_Particles = new Image[particleCount];
                for (var index = 0; index < m_Particles.Length; index++)
                {
                    m_Particles[index] = Instantiate(flyingIcon, flyingIcon.transform.parent);
                    m_Particles[index].name = "ui_image_reward_burst_" + index + "_value";
                    m_Particles[index].raycastTarget = false;
                    m_Particles[index].maskable = false;
                }
            }

            if (m_Glow == null && glowSprite != null)
            {
                m_Glow = Instantiate(flyingIcon, flyingIcon.transform.parent);
                m_Glow.name = "ui_image_reward_glow_value";
                m_Glow.transform.SetSiblingIndex(flyingIcon.transform.GetSiblingIndex());
                m_Glow.raycastTarget = false;
                m_Glow.maskable = false;
            }
        }

        private void HideSourceIcon(WheelSliceView slice)
        {
            m_Source = slice.IconRect.GetComponent<Image>();
            m_SourceWasEnabled = m_Source.enabled;
            m_Source.enabled = false;
            var sourceScale = slice.IconRect.lossyScale;
            var parentScale = flyingIcon.transform.parent.lossyScale;
            m_IconScale = new Vector3(sourceScale.x / parentScale.x, sourceScale.y / parentScale.y, 1f);
        }

        private void PrepareGlow(WheelSliceView slice)
        {
            if (m_Glow != null)
            {
                ConfigureIcon(m_Glow, slice, m_IconScale * m_GlowStartScale);
                m_Glow.sprite = glowSprite;
                var size = Mathf.Max(slice.IconRect.rect.width, slice.IconRect.rect.height);
                m_Glow.rectTransform.sizeDelta = Vector2.one * size;
                m_Glow.color = glowColor;
            }
        }

        private static void ConfigureIcon(Image image, WheelSliceView slice, Vector3 scale)
        {
            image.sprite = slice.Icon;
            image.rectTransform.sizeDelta = slice.IconRect.rect.size;
            image.rectTransform.position = slice.IconRect.TransformPoint(slice.IconRect.rect.center);
            image.rectTransform.rotation = Quaternion.identity;
            image.rectTransform.localScale = scale;
            image.gameObject.SetActive(true);
        }

        private void OnDisable() => ResetVisuals();
    }
}
