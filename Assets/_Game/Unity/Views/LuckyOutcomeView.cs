using DG.Tweening;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class LuckyOutcomeView : SliceOutcomeView
    {
        [SerializeField, Min(.01f)] private float duration = .5f;
        private WheelSliceView m_Slice;

        public override bool CanPresent(SliceData slice) => slice is LuckySliceData;

        public override Tween Play(WheelSliceView slice)
        {
            m_Slice = slice;
            return DOTween.Sequence()
                .Append(slice.Motion.DOScale(1.3f, duration * .4f).SetEase(Ease.OutBack))
                .Append(slice.Motion.DOScale(1f, duration * .6f).SetEase(Ease.InOutSine));
        }

        public override void ResetVisuals()
        {
            if (m_Slice != null)
                m_Slice.ResetMotion();
            m_Slice = null;
        }
    }
}
