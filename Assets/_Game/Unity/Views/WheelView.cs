using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Presentation._Game.Presentation.ViewContracts;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Runtime._Game.Runtime.Session;
using Vertigo.Unity._Game.Unity.Authoring;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class WheelView : MonoBehaviour, ISpinView
    {
        [SerializeField] private RectTransform motion;
        [SerializeField] private Image wheel;
        [SerializeField] private Image indicator;
        [SerializeField] private WheelSliceView[] slices;
        [SerializeField] private SliceOutcomeView[] outcomes;
        [SerializeField] private WheelPointerView pointer;
        [SerializeField] private WheelTierTransition tierTransition;
        [SerializeField, Min(0.01f)] private float spinDuration = 4;
        [SerializeField, Min(1)] private int revolutions = 5;
        [SerializeField, Min(1f)] private float acceleratedSpinSpeed = 16f;
        [SerializeField, Min(0.01f)] private float spinSlowdownDuration = 1.8f;
        [SerializeField, Min(1f)] private float acceleratedRewardSpeed = 4f;
        private readonly TweenOperation m_Operation = new TweenOperation();
        private WheelConfigurationAsset m_Configuration;
        private Round m_Round;
        private bool m_IsSpinning;
        private bool m_SpinAccelerated;
        private float m_RewardSpeed = 1f;
        private SliceOutcomeView m_Outcome;
        public int SliceCount => slices.Length;
        public float Rotation => motion.localEulerAngles.z;
        public bool CanAccelerate => m_Operation.IsPlaying &&
            (m_IsSpinning || (m_Outcome != null && m_Outcome.CanAccelerate));

        public void Initialize(WheelConfigurationAsset configuration)
        {
            m_Configuration = configuration;
            foreach (var outcome in outcomes)
                outcome.ResetVisuals();
        }

        public void Bind(Round round)
        {
            if (ReferenceEquals(m_Round, round))
                return;
            if (round.Slices.Count != SliceCount)
                throw new InvalidOperationException("Wheel capacity does not match the round.");
            tierTransition.Finish();
            var animate = m_Round != null && m_Round.Tier != round.Tier && round.Zone > 1 &&
                isActiveAndEnabled && tierTransition.isActiveAndEnabled;
            m_Round = round;
            if (animate)
                tierTransition.Play(m_Configuration.GetTier(round.Tier), () => ApplyRound(round));
            else
                ApplyRound(round);
        }

        private void ApplyRound(Round round)
        {
            var tier = m_Configuration.GetTier(round.Tier);
            wheel.sprite = tier.Wheel;
            indicator.sprite = tier.Indicator;
            motion.localRotation = Quaternion.identity;
            for (var index = 0; index < slices.Length; index++)
            {
                slices[index].Bind(round.Slices[index], tier.GetSlice(round.SourceIndices[index]));
                slices[index].KeepUpright(0);
            }
        }

        public void Spin(SpinResult result, Action<VisualResult> completed)
        {
            tierTransition.Finish();
            m_IsSpinning = true;
            m_SpinAccelerated = false;
            m_RewardSpeed = 1f;
            m_Outcome = null;
            var angle = motion.localEulerAngles.z;
            var targetAngle = result.WinningIndex * 360f / SliceCount;
            var target = angle - revolutions * 360f - Mathf.Repeat(angle - targetAngle, 360f);
            var lastBoundary = Mathf.FloorToInt((-angle + 180f / SliceCount) / (360f / SliceCount));
            m_Operation.Play(DOTween.To(() => angle, value =>
            {
                angle = value;
                motion.localRotation = Quaternion.Euler(0, 0, value);
                foreach (var slice in slices)
                    slice.KeepUpright(value);
                var boundary = Mathf.FloorToInt((-value + 180f / SliceCount) / (360f / SliceCount));
                if (boundary != lastBoundary)
                {
                    lastBoundary = boundary;
                    pointer.Tick();
                }
            }, target, spinDuration).SetEase(Ease.OutQuart), status =>
            {
                m_IsSpinning = false;
                completed(status);
            });
        }

        public void Highlight(SpinResult result, Action<VisualResult> completed)
        {
            var outcome = FindOutcome(result.WinningSlice);
            m_Outcome = outcome;
            m_Operation.Play(outcome.Highlight(slices[result.WinningIndex]), completed);
        }

        public void PlayOutcome(SpinResult result, Action<VisualResult> completed)
        {
            var outcome = FindOutcome(result.WinningSlice);
            m_Operation.Play(outcome.Play(slices[result.WinningIndex]), status =>
            {
                outcome.ResetVisuals();
                m_Outcome = null;
                m_RewardSpeed = 1f;
                completed(status);
            });
            m_Operation.Accelerate(m_RewardSpeed);
        }

        public void Accelerate()
        {
            if (!CanAccelerate)
                return;
            if (m_IsSpinning)
            {
                if (m_SpinAccelerated)
                    return;
                m_SpinAccelerated = true;
                m_Operation.Accelerate(acceleratedSpinSpeed, spinSlowdownDuration);
                return;
            }

            m_RewardSpeed = acceleratedRewardSpeed;
            m_Outcome.SetPlaybackSpeed(m_RewardSpeed);
            m_Operation.Accelerate(m_RewardSpeed);
        }

        private SliceOutcomeView FindOutcome(RoundSlice slice)
        {
            foreach (var outcome in outcomes)
                if (outcome.CanPresent(slice))
                    return outcome;
            throw new InvalidOperationException("No view can present this slice.");
        }

        public void Cancel()
        {
            m_IsSpinning = false;
            m_SpinAccelerated = false;
            m_RewardSpeed = 1f;
            m_Outcome = null;
            tierTransition.Finish();
            m_Operation.Cancel();
            pointer.ResetMotion();
            foreach (var slice in slices)
                slice.ResetMotion();
            foreach (var outcome in outcomes)
                outcome.ResetVisuals();
        }

        private void OnDisable() => Cancel();
    }
}
