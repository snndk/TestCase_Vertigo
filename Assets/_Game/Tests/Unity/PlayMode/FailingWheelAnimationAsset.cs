#if UNITY_EDITOR
using System;
using DG.Tweening;
using UnityEngine;
using Vertigo.Unity._Game.Unity.Authoring;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Tests.Unity.Assets._Game.Tests.Unity.PlayMode
{
    public class FailingWheelAnimationAsset : GoldWheelAnimationAsset
    {
        private float m_Value;
        public bool FailOnArrival { get; set; }
        public bool FailureEnabled { get; set; } = true;
        public Sequence FailedSequence { get; private set; }
        public Tween FailedTween { get; private set; }
        public bool SequenceKilled { get; private set; }
        public bool TweenKilled { get; private set; }
        public bool CompletionRan { get; private set; }
        public InvalidOperationException Failure { get; } = new InvalidOperationException("Test animation construction failed.");

        public override void InsertHighlight(Sequence sequence, RewardCelebrationView view,
            RectTransform source, WheelTierAsset tier)
        {
            base.InsertHighlight(sequence, view, source, tier);
            if (FailureEnabled && !FailOnArrival)
                Fail(sequence);
        }

        public override void InsertArrival(Sequence sequence, float arrivalTime, RewardCelebrationView view,
            RectTransform target, WheelTierAsset tier)
        {
            base.InsertArrival(sequence, arrivalTime, view, target, tier);
            if (FailureEnabled && FailOnArrival)
                Fail(sequence);
        }

        private void Fail(Sequence sequence)
        {
            FailedSequence = sequence;
            FailedTween = DOTween.To(() => m_Value, value => m_Value = value, 1f, 0.5f);
            sequence.onKill += () => SequenceKilled = true;
            FailedTween.OnKill(() => TweenKilled = true);
            sequence.Insert(0, FailedTween);
            sequence.InsertCallback(0.5f, () => CompletionRan = true);
            sequence.SetUpdate(UpdateType.Manual);
            throw Failure;
        }
    }
}
#endif
