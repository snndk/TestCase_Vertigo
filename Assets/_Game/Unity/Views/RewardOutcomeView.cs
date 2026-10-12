using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class RewardOutcomeView : SliceOutcomeView
    {
        [SerializeField] private Image flyingIcon;
        [SerializeField] private RewardListView rewards;
        [SerializeField, Min(0.01f)] private float duration = 0.45f;
        public override bool CanPresent(RoundSlice slice) => slice is RewardSlice;
        public override Tween Play(WheelSliceView slice)
        {
            var reward = ((RewardSlice)slice.Slice).Reward;
            var target = rewards.PrepareArrival(reward.RewardId);
            flyingIcon.sprite = slice.Icon;
            SpriteLayout.Fit(flyingIcon, new Vector2(70, 70));
            flyingIcon.gameObject.SetActive(true);
            flyingIcon.transform.position = slice.IconRect.position;
            return DOTween.To(() => flyingIcon.transform.position,
                value => flyingIcon.transform.position = value, target.position, duration).SetEase(Ease.InOutQuad);
        }
        public override void ResetVisuals() => flyingIcon.gameObject.SetActive(false);
    }
}
