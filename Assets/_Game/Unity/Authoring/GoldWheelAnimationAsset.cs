using DG.Tweening;
using UnityEngine;
using Vertigo.Unity._Game.Unity.Views;

namespace Vertigo.Unity._Game.Unity.Authoring
{
    [CreateAssetMenu(menuName = "Vertigo/Wheel animations/Gold")]
    public class GoldWheelAnimationAsset : WheelAnimationAsset
    {
        [SerializeField, Range(0f, 1f)] private float celebrationStrength = 1f;
        [SerializeField] private string celebrationMessage = "GOLD ZONE";

        public override void BuildEntrance(Sequence sequence, WheelTierTransition view, WheelTierAsset tier)
        {
            view.AppendTransition(sequence, tier, 1f + celebrationStrength, celebrationStrength * 2f);
            sequence.Insert(0, view.CreateCelebration(celebrationMessage, tier.TransitionColor, celebrationStrength));
        }

        public override void InsertHighlight(Sequence sequence, RewardCelebrationView view,
            RectTransform source, WheelTierAsset tier)
        {
            sequence.Insert(0, view.CreateHighlight(source, tier.TransitionColor, celebrationStrength));
        }

        public override void InsertArrival(Sequence sequence, float arrivalTime, RewardCelebrationView view,
            RectTransform target, WheelTierAsset tier)
        {
            sequence.Insert(arrivalTime, view.CreateArrival(target, tier.TransitionColor, celebrationStrength));
        }
    }
}
