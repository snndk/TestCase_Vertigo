using DG.Tweening;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Views
{
    public abstract class SliceOutcomeView : MonoBehaviour
    {
        public virtual bool CanAccelerate => false;
        public virtual void SetPlaybackSpeed(float speed) { }
        public abstract bool CanPresent(RoundSlice slice);
        public virtual Tween Highlight(WheelSliceView slice)
        {
            return DOTween.Sequence()
                .Append(slice.Motion.DOScale(1.15f, 0.12f))
                .Append(slice.Motion.DOScale(1f, 0.12f));
        }

        public abstract Tween Play(WheelSliceView slice);
        public abstract void ResetVisuals();
    }
}
