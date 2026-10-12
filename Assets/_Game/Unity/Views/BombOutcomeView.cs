using DG.Tweening;
using UnityEngine;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class BombOutcomeView : SliceOutcomeView
    {
        [SerializeField] private CanvasGroup flash;
        public override bool CanPresent(SliceData slice) => slice is BombSliceData;
        public override Tween Play(WheelSliceView slice)
        {
            flash.alpha = 0;
            return DOTween.Sequence()
                .Append(DOTween.To(() => flash.alpha, value => flash.alpha = value, 0.65f, 0.16f))
                .Append(DOTween.To(() => flash.alpha, value => flash.alpha = value, 0, 0.3f));
        }
        public override void ResetVisuals() => flash.alpha = 0;
    }
}
