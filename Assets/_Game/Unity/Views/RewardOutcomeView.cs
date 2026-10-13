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
        [SerializeField] private RewardCelebrationView celebration;
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
        private const float m_GlowEndScale = 2.8f;
        private const float m_ParticleArcScale = 0.75f;
        private const float m_ParticleArcStep = 0.08f;
        private const float m_MainIconArcScale = 1.2f;
        private const float m_MainIconArrivalScale = 0.55f;
        private const float m_ArrivalSettleDuration = 0.18f;
        private RewardEffectIcons m_Icons;
        private RewardRowView m_ArrivalRow;
        private Tween m_Animation;

        private RewardEffectIcons Icons => m_Icons ?? (m_Icons = new RewardEffectIcons(flyingIcon));

        public override bool CanPresent(SliceData slice) => slice is RewardSliceData;
        public override bool CanAccelerate => true;

        public override void SetPlaybackSpeed(float speed)
        {
            if (m_ArrivalRow != null)
                m_ArrivalRow.SetPlaybackSpeed(speed);
        }

        public override Tween Highlight(WheelSliceView slice)
        {
            ResetVisuals();
            Icons.Prepare(slice, particleCount, glowSprite, glowColor);
            var reward = ((RewardSliceData)slice.Slice).Reward;
            m_ArrivalRow = rewards.PrepareArrival(reward.RewardId);
            var sequence = DOTween.Sequence();
            sequence.Append(flyingIcon.rectTransform.DOScale(Icons.Scale * enlargedScale, burstDuration)
                .SetEase(Ease.OutBack, m_HighlightOvershoot));
            AddParticleBurst(sequence);
            AddGlow(sequence);
            if (slice.Tier != null && slice.Tier.CelebrationStrength > 0)
                sequence.Insert(0, celebration.CreateHighlight(slice.IconRect,
                    slice.Tier.TransitionColor, slice.Tier.CelebrationStrength));
            sequence.Insert(0, rewards.RevealArrival(m_ArrivalRow));
            sequence.AppendInterval(burstHold);
            return TrackAnimation(sequence);
        }

        private void AddParticleBurst(Sequence sequence)
        {
            var center = flyingIcon.rectTransform.localPosition;
            var radius = Mathf.Max(flyingIcon.rectTransform.rect.width * Icons.Scale.x,
                flyingIcon.rectTransform.rect.height * Icons.Scale.y);
            foreach (var image in Icons.Particles)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var distance = radius * Random.Range(spreadRadius.x, spreadRadius.y);
                var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var particle = image.rectTransform;
                sequence.Insert(0, particle.DOLocalMove(center + offset, burstDuration).SetEase(Ease.OutCubic));
                sequence.Insert(0, particle.DOScale(Icons.Scale * particleScale,
                    burstDuration * m_ParticleGrowthDurationRatio).SetEase(Ease.OutBack));
            }
        }

        private void AddGlow(Sequence sequence)
        {
            if (Icons.Glow == null)
                return;

            var transparent = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            sequence.Insert(0, Icons.Glow.rectTransform.DOScale(Icons.Scale * m_GlowEndScale, burstDuration)
                .SetEase(Ease.OutQuad));
            sequence.Insert(0, DOTween.To(() => Icons.Glow.color, value => Icons.Glow.color = value,
                transparent, burstDuration).SetEase(Ease.InQuad));
        }

        public override Tween Play(WheelSliceView slice)
        {
            var target = m_ArrivalRow.IconRect;
            var sequence = DOTween.Sequence();
            AddParticleFlights(sequence, target);

            var mainIconDepartureTime = Icons.Particles.Count * flightStagger;
            AddFlight(sequence, flyingIcon, target, mainIconDepartureTime, m_MainIconArcScale);
            sequence.Insert(mainIconDepartureTime,
                flyingIcon.rectTransform.DOScale(Icons.Scale * m_MainIconArrivalScale, flightDuration));

            var firstArrivalTime = flightDuration;
            var lastArrivalTime = mainIconDepartureTime + flightDuration;
            var countDuration = lastArrivalTime - firstArrivalTime;
            var reward = ((RewardSliceData)slice.Slice).Reward;
            sequence.Insert(firstArrivalTime, m_ArrivalRow.CreateArrivalCount(reward.Amount, countDuration));
            if (slice.Tier != null && slice.Tier.CelebrationStrength > 0)
                sequence.Insert(lastArrivalTime, celebration.CreateArrival(target,
                    slice.Tier.TransitionColor, slice.Tier.CelebrationStrength));
            sequence.AppendInterval(m_ArrivalSettleDuration);
            return TrackAnimation(sequence);
        }

        private Tween TrackAnimation(Tween animation)
        {
            m_Animation = animation;
            animation.OnComplete(() => ReleaseAnimation(animation));
            animation.OnKill(() => ReleaseAnimation(animation));
            return animation;
        }

        private void ReleaseAnimation(Tween animation)
        {
            if (ReferenceEquals(m_Animation, animation))
                m_Animation = null;
        }

        private void AddParticleFlights(Sequence sequence, RectTransform target)
        {
            for (var index = 0; index < Icons.Particles.Count; index++)
            {
                var departureTime = index * flightStagger;
                var arcScale = m_ParticleArcScale + index * m_ParticleArcStep;
                AddFlight(sequence, Icons.Particles[index], target, departureTime, arcScale);
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
            if (m_ArrivalRow != null)
                m_ArrivalRow.ResetArrival();
            m_ArrivalRow = null;
            Icons.Reset();
            if (celebration != null)
                celebration.ResetVisuals();
        }

        private void OnDisable() => ResetVisuals();
    }
}
